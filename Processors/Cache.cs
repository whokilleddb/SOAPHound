using System.Collections.Concurrent;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using SOAPHound.Enums;
using SOAPHound.OutputTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Linq;
using System.Diagnostics;

namespace SOAPHound.Processors
{
    

    // We're using the WCF datacontract to serialize the cache as a JSON object

    public static class Cache
    {
        static Cache()
        {
            ValueToIdCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); //added OrdinalIgnoreCase to use case insensitive comparisons for gplink->gpo
            IdToTypeCache = new Dictionary<string, Label>();
        }

        // This class is here to work aroud the limitation of NewtonSoft in deserializing static classes.
        [DataContract]
        internal class SerializeableCache
        {
            [DataMember] public Dictionary<string, Label> IdToTypeCache { get; set; }

            [DataMember] public Dictionary<string, string> ValueToIdCache { get; set; }
        }

        public class CacheContractResolver : DefaultContractResolver
        {
            private static readonly CacheContractResolver Instance = new CacheContractResolver();
            public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings()
            {
                ContractResolver = Instance
            };

            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var prop = base.CreateProperty(member, memberSerialization);
                if (!prop.Writable && (member as PropertyInfo)?.GetSetMethod(true) != null)
                {
                    prop.Writable = true;
                }
                return prop;
            }

        }

        public static void Deserialize(string id, string exporturl)
        {
            if (string.IsNullOrEmpty(id)) {
                Console.WriteLine("No Valid Cache ID found");
                return;
            }
            string cache_json;
            cache_json = PostToUrl.FetchCache(exporturl, id);
            if (string.IsNullOrEmpty(cache_json))
            {
                Console.WriteLine("Failed to fetch Cache from "+ exporturl);
                return;
            }
            var json = Base64Encoder.DecodeFromBase64(cache_json);
            if (string.IsNullOrEmpty(json))
            {
                Console.WriteLine("Failed to decode cache id: " + id);
                return;
            }
            // Console.WriteLine(cache_json);
            // var json = File.ReadAllText(path);
            SerializeableCache tempCache = JsonConvert.DeserializeObject<SerializeableCache>(json, CacheContractResolver.Settings);
            Cache.ValueToIdCache = new Dictionary<string,string>(tempCache.ValueToIdCache, StringComparer.OrdinalIgnoreCase);
            Cache.IdToTypeCache = tempCache.IdToTypeCache;
        }

        public static string Serialize(string exportURL)
        {
            SerializeableCache tempCache = new SerializeableCache();
            tempCache.IdToTypeCache = Cache.IdToTypeCache;
            tempCache.ValueToIdCache = Cache.ValueToIdCache;
            var serialized = JsonConvert.SerializeObject(tempCache);
            string id = Randomizer.GenerateRandomString(32);
            if (PostToUrl.PostMessage(exportURL + "/cache?id=" + id, serialized))
            {
                Console.WriteLine("Exported cache to: " + exportURL + "/cache?id="+id);
                Console.WriteLine("CACHE ID: " + id);
                
            } else
            {
                Console.WriteLine("Failed to export cache to: " + exportURL + "/cache");
                id = null;
            }
            // File.WriteAllText(path, serialized);
            return id;
        }

        public static Dictionary<string, Label> IdToTypeCache { get; private set; }

        public static Dictionary<string, string> ValueToIdCache { get; private set; }



        internal static void AddConvertedValue(string key, string value)
        {
            if (ValueToIdCache.ContainsKey(key))
            {
                Console.WriteLine("Duplicate key found with value: " + key);
            }
            else
            {
                ValueToIdCache.Add(key, value);
            }
        }

   
        internal static void AddType(string key, Label value)
        {
            if (IdToTypeCache.ContainsKey(key))
            {
                Console.WriteLine("Duplicate key found with value: " + key);
            }
            else
            {
                IdToTypeCache.Add(key, value);
            }
        }

        internal static bool GetConvertedValue(string key, out string value)
        {
            return ValueToIdCache.TryGetValue(key, out value);
        }

        //internal static bool GetPrefixedValue(string key, string domain, out string value)
        //{
        //    return ValueToIdCache.TryGetValue(GetPrefixKey(key, domain), out value);
        //}

        internal static bool GetIDType(string key, out Label value)
        {
            if (!IdToTypeCache.TryGetValue(key, out value))
            {
                value = Label.Base;
                return false;
            }
            else
            {
                return true;
            }
        }

        internal static bool GetChildObjects(string dn, out TypedPrincipal[] childObjects)
        {
            childObjects = new TypedPrincipal[] { };
            var matchingKeysAll = ValueToIdCache.Where(kvp => kvp.Key.Contains(dn)).Select(kvp => kvp.Key);
            var matchingKeys = matchingKeysAll.Where(key => key != dn).ToList();


            
            foreach (string matchingKey in matchingKeys)
            {
                if (IsDistinguishedNameFiltered(matchingKey))
                    continue;
               
                TypedPrincipal childObject = new TypedPrincipal { };
                if (GetConvertedValue(matchingKey, out var id) && GetIDType(id, out var type))
                {
                    childObject = new TypedPrincipal
                    {
                        ObjectIdentifier = id.ToUpper(),
                        ObjectType = type
                    };
                    childObjects = childObjects.Append(childObject).ToArray();
                }
                else
                    continue;
            }

            if (matchingKeys == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }


        internal static bool GetDomainChildObjects(string dn, out TypedPrincipal[] childObjects)
        {
            int dnlevel = dn.Count(f => f == '=');
            childObjects = new TypedPrincipal[] { };
            var matchingKeysAll = ValueToIdCache.Where(kvp => kvp.Key.Contains(dn)).Select(kvp => kvp.Key);
            var matchingKeys = matchingKeysAll.Where(key => key != dn).ToList();



            foreach (string matchingKey in matchingKeys)
            {
                //Getting one sublevel of data for the domain child objects
                if (matchingKey.Count(f => f == '=') != (dnlevel + 1))
                    continue;

                if (IsDistinguishedNameFiltered(matchingKey))
                    continue;

                TypedPrincipal childObject = new TypedPrincipal { };
                if (GetConvertedValue(matchingKey, out var id) && GetIDType(id, out var type))
                {
                    childObject = new TypedPrincipal
                    {
                        ObjectIdentifier = id.ToUpper(),
                        ObjectType = type
                    };
                    childObjects = childObjects.Append(childObject).ToArray();
                }
                else
                    continue;
            }

            if (matchingKeys == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private static bool IsDistinguishedNameFiltered(string distinguishedName)
        {
            var dn = distinguishedName.ToUpper();
            if (dn.Contains("CN=PROGRAM DATA,DC=")) return true;

            if (dn.Contains("CN=SYSTEM,DC=")) return true;

            return false;
        }

        private static string GetPrefixKey(string key, string domain)
        {
            return $"{key}|{domain}";
        }
        public static string GetCacheStats()
        {
            try
            {
                return
                    $"{IdToTypeCache.Count} ID to type mappings.\n {ValueToIdCache.Count} name to SID mappings.\n";
            }
            catch
            {
                return "";
            }
        }



    }
}
