﻿using System;
using System.Collections.Concurrent;
using System.Reflection;
using Common.Logging;
using Nabbix.Items;

namespace Nabbix
{
    public class ItemRegistry
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof (ItemRegistry));

        public ConcurrentDictionary<string, Item> RegisteredProperties { get; }

        public ItemRegistry()
        {
            RegisteredProperties = new ConcurrentDictionary<string, Item>();
        }

        public void RegisterInstance(object instance)
        {
            foreach (PropertyInfo property in instance.GetType().GetProperties())
            {
                var attribute = property.GetCustomAttribute<NabbixItemAttribute>();
                if (attribute != null)
                {
                    foreach (var key in attribute.ZabbixItemKeys)

                        if (key != null)
                        {
                            RegisteredProperties.TryAdd(key, new Item(property, attribute, instance));
                        }
                        else
                        {
                            Log.WarnFormat("NabbixItemAttribute - Missing key for object {0}, {1}",
                                instance.GetType(), property.Name);
                        }
                }
            }
        }

        public string GetItemValue(string key)
        {
            key = key.Trim();

            if (key == "agent.ping")
                return "1";

            try
            {
                if (WindowsPerformanceCounters.IsCounter(key))
                {
                    return WindowsPerformanceCounters.GetNextValue(key);
                }

                Item item;
                if (RegisteredProperties.TryGetValue(key, out item))
                {
                    return item.GetValue(key);
                }
            }
            catch (Exception e)
            { 
                Log.ErrorFormat("Exception occurred querying key {0}", e, key);
            }



            return Item.NotSupported;
        }
    }
}