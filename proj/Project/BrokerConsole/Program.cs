﻿using DADInterfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;

namespace BrokerConsole
{
    class BrokerRemote:MarshalByRefObject, Broker
    {
        private PuppetMaster _pm;
        // personal information
        private string _name;
        private string _site;
        private string _uri;
        private bool _isRoot;
        // site information
        private List<Publisher> _publishers = new List<Publisher>();
        private List<Subscriber> _subscribers = new List<Subscriber>();
        private List<Site> _childSites = new List<Site>();
        private Site _parentSite;
        //
        Dictionary<String, List<Subscriber>> _subscriptions = new Dictionary<string, List<Subscriber>>();
        Dictionary<string, int> _brokersSeqNums = new Dictionary<string, int>();

        public BrokerRemote(PuppetMaster pm, string name, string site)
        {
            _name = name;
            _pm = pm;
            _site = site;
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Started Broker");
            int nargs = 4;
            if (args.Length != nargs)
            {
                Console.WriteLine("Expected {0} arguments, got {1}", nargs, args.Length);
                Console.Read();
                return;
            }
            string puppetMasterURI = args[0];
            string name = args[1];
            string site = args[2];
            int port = int.Parse(args[3]);

            string channelURI = Utility.setupChannel(port);           

            // get the puppetMaster that started this process
            PuppetMaster pm = (PuppetMaster)Activator.GetObject(typeof(PuppetMaster), puppetMasterURI);
            BrokerRemote broker = new BrokerRemote(pm,name, site);
            //we need to register each remote object
            ObjRef o = RemotingServices.Marshal(broker, name, typeof(Broker));
            broker.setURI(string.Format("{0}/{1}", channelURI, name));            
            Console.WriteLine("Created Broker at \"{0}\"", broker.getURI());
            
            //now that broker is created and marshalled
            //send remote to puppetMaster which is Monitor.waiting for the remote            
            pm.registerBroker(broker);
            Console.WriteLine("Just registered at puppetMaster");
            Console.WriteLine("Press key to leave");
            Console.Read();
        }
       

        public void crash()
        {
            throw new NotImplementedException();
        }

        public void freeze()
        {
            throw new NotImplementedException();
        }

        public string getName()
        {
            return _name;
        }

        public string getSite()
        {
            return _site;
        }

        public string getURI()
        {
            return _uri;
        }

        public void setURI(string v)
        {
            _uri = v;
        }

        public void setChildren(List<Site> child_sites)
        {
            _childSites = child_sites;
        }

        public void setParent(Site parent_site)
        {
            _parentSite = parent_site;
        }

        public string status()
        {
            throw new NotImplementedException();
        }

        public void unfreeze()
        {
            throw new NotImplementedException();
        }

        public void setPublishers(List<Publisher> site_publishers)
        {
            _publishers = site_publishers;
        }

        public void setSubscribers(List<Subscriber> site_subscribers)
        {
            _subscribers = site_subscribers;
        }

        private bool isDuplicate(SubscribeMessage msg)
        {
            if (!_brokersSeqNums.ContainsKey(msg.uri))
            {
                _brokersSeqNums.Add(msg.uri, 0);
            }
            if (msg.seqnum < _brokersSeqNums[msg.uri])
            {
                // its a duplicated request, discard
                // TODO log
                return true;
            }
            // if its waiting for 0 and receives 4, then next is 5, is this desired?
            _brokersSeqNums[msg.uri] = msg.seqnum + 1;
            return false;
        }
        public void subscribe(SubscribeMessage msg)
        {
            if (isDuplicate(msg)) return;


            if (!_subscriptions.ContainsKey(msg.topic))
            {
                _subscriptions.Add(msg.topic, new List<Subscriber>());
            }
            _subscriptions[msg.topic].Add(msg.sub);
            // TODO suppot * wildcard on topic
        }

        public void unsubscribe(UnsubscribeMessage msg)
        {
            if (isDuplicate(msg)) return;

            if (_subscriptions.ContainsKey(msg.topic))
            {
                _subscriptions[msg.topic].Remove(msg.sub);
            }
            // TODO

        }

        public void publish(PublishMessage msg)
        {
            // FLOODING implementation
            // TODO discart if duplicate message
            // TODO make all calls assyncs

            log(string.Format("[Subscribe] Received event {0}",msg));
            // send to site subscribers
            foreach(Subscriber s in _subscribers)
            {
                s.receive(msg.topic, msg.content);
            }

            // send to child sites brokers
            string last_broker = msg.last_broker;
            msg.last_broker = getURI();
            foreach(Site s in _childSites)
            {
                foreach(Broker b in s.brokers)
                {
                    if(b.getURI() != last_broker)
                    {
                        b.publish(msg);
                    }
                }
            }
            // send to parent site brokers
            if(_parentSite != null)
            {
                foreach (Broker b in _parentSite.brokers)
                {
                    if (b.getURI() != last_broker)
                    {
                        b.publish(msg);
                    }
                }
            }
            
        }

        void log(string e)
        {
            _pm.reportEvent(getURI(), e);
            Console.WriteLine(e);
        }

        public void setIsRoot(bool v)
        {
            _isRoot = true;
        }

        public bool getIsRoot()
        {
            return _isRoot;
        }
    }
}
