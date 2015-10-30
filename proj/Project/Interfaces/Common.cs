﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;

namespace DADInterfaces
{
    public enum RoutingPolicy { flooding, filter};
    public enum OrderingPolicy { no, fifo, total};

    // A Node can be a Broker, Publisher or Subscriber
    public interface Node
    {
        string getURI();
        string status();
        string getName();
        string getSite();

        void crash();
        void freeze();
        void unfreeze();
    }

    public delegate void PropagateSubscribeDelegate(PropagatedSubcribeMessage msg);
    public delegate void PropagateUnsubscribeDelegate(PropagatedUnsubscribeMessage msg);
    public delegate void PropagatePublishDelegate(PropagatedPublishMessage msg);
    public interface Broker: Node
    {
        void setIsRoot();
        bool getIsRoot();
        void setOrderingPolicy(OrderingPolicy p);
        void setRoutingPolicy(RoutingPolicy p);

        void setParent(Site parent_site);
        void setChildren(List<Site> child_sites);
        void setPublishers(List<Publisher> site_publishers);
        void setSubscribers(List<Subscriber> site_subscribers);

        // methods called by Subscriber
        void subscribe(SubscribeMessage msg);
        void unsubscribe(UnsubscribeMessage msg);
        // method called by Publisher
        void publish(PublishMessage msg);
        // methods called by other Brokers
        void propagateSubscribe(PropagatedSubcribeMessage msg);
        void propagateUnsubscribe(PropagatedUnsubscribeMessage msg);
        void propagatePublish(PropagatedPublishMessage msg);
            
    }
    public interface Publisher: Node
    {
        void setSiteBroker(Broker site_broker);
        void publish(string topic, string msg, int quantity, int interval);
        
    }
    public delegate void ReceiveDelegate(string topic, string content);
    public interface Subscriber: Node
    {
        void setSiteBroker(Broker site_broker);
        void subscribe(string topic);
        void unsubscribe(string topic);
        void receive(string topic, string content);
    }

    public interface ICoordinator
    {
        void reportEvent(string type, string uri1, string uri2, string topic, string seqnum);
    }

    public interface PuppetMaster
    {
        List<Broker> getBrokers();
        List<Subscriber> getSubscribers();
        List<Publisher> getPublishers();

        Broker createBroker(string name,string site,int port);
        Publisher createPublisher(string name,string site,int port);
        Subscriber createSubscriber(string name,string site,int port);

        // When a PuppetMaster createX it returns a remoteObjectX
        // But the only way to get a remoteObjectX is to wait
        // for the created process to call remoteObjectX on the PuppetMasterRemote
        // which the process activated using the URI given in main arguments
        void registerBroker(Broker b);
        void registerPublisher(Publisher p);
        void registerSubscriber(Subscriber s);
        string status();

        // used by all processes created by the puppet master to report events
        // type = {PubEvent, BroEvent, SubEvent}
        void reportEvent(string type, string uri1, string uri2, string topic, string seqnum);
        void reportEvent(string a, String b);
    }

    public static class Utility
    {
        public static string setupChannel(int port)
        {
            //create process channel
            BinaryServerFormatterSinkProvider ssp = new BinaryServerFormatterSinkProvider();
            ssp.TypeFilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full;
            IDictionary props = new Hashtable();
            props["port"] = port;
            TcpChannel channel = new TcpChannel(props, null, ssp);
            ChannelServices.RegisterChannel(channel, true);

            // print uris
            ChannelDataStore cds = (ChannelDataStore)channel.ChannelData;
            string channelURI = cds.ChannelUris[0];
            Console.WriteLine("Opened remoting channel at \"{0}\"", channelURI);
            return channelURI;
        }
    }

}
