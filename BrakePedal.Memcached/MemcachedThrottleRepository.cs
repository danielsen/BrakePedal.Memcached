using System;
using System.Linq;
using System.Collections.Generic;
using Enyim.Caching;

namespace BrakePedal.Memcached
{
    public class MemcachedThrottleRepository : IThrottleRepository
    {
        private readonly IMemcachedClient _memcachedClient;

        public object[] PolicyIdentityValues { get; set; }

        public MemcachedThrottleRepository(IMemcachedClient memcachedClient)
        {
            _memcachedClient = memcachedClient;
        }

        public long? GetThrottleCount(IThrottleKey key, Limiter limiter)
        {
            string id = CreateThrottleKey(key, limiter);
            var value = _memcachedClient.Get<string>(id);

            if (long.TryParse(value, out long target))
                return target;
            return null;
        }

        public void AddOrIncrementWithExpiration(IThrottleKey key, Limiter limiter)
        {
            string id = CreateThrottleKey(key, limiter);
            ulong result = _memcachedClient.Increment(id, 1, 1, limiter.Period);

            if (result == 1)
                _memcachedClient.Increment(id, 1, 0, limiter.Period);
        }

        public bool LockExists(IThrottleKey key, Limiter limiter)
        {
            string id = CreateLockKey(key, limiter);
            return _memcachedClient.TryGet(id, out _);
        }

        public void SetLock(IThrottleKey key, Limiter limiter)
        {
            string id = CreateLockKey(key, limiter);
            _memcachedClient.Increment(id, 1, 1, limiter.LockDuration.Value);
        }

        public void RemoveThrottle(IThrottleKey key, Limiter limiter)
        {
            string id = CreateThrottleKey(key, limiter);
            _memcachedClient.Remove(id);
        }

        public string CreateThrottleKey(IThrottleKey key, Limiter limiter)
        {
            List<object> values = CreateBaseKeyValues(key, limiter);

            string countKey = TimeSpanToFriendlyString(limiter.Period);
            values.Add(countKey);

            if (limiter.Period.TotalSeconds == 1)
                values.Add(GetUnixTimestamp());

            string id = string.Join(":", values);
            return id;
        }

        public string CreateLockKey(IThrottleKey key, Limiter limiter)
        {
            List<object> values = CreateBaseKeyValues(key, limiter);

            string lockKeySuffix = TimeSpanToFriendlyString(limiter.LockDuration.Value);
            values.Add("lock");
            values.Add(lockKeySuffix);

            string id = string.Join(":", values);
            return id;
        }

        private string TimeSpanToFriendlyString(TimeSpan span)
        {
            var items = new List<string>();
            Action<double, string> ifNotZeroAppend = (value, key) =>
            {
                if (value != 0)
                    items.Add(string.Concat(value, key));
            };

            ifNotZeroAppend(span.Days, "d");
            ifNotZeroAppend(span.Hours, "h");
            ifNotZeroAppend(span.Minutes, "m");
            ifNotZeroAppend(span.Seconds, "s");

            return string.Join("", items);
        }
        
        private List<object> CreateBaseKeyValues(IThrottleKey key, Limiter limiter)
        {
            List<object> values = key.Values.ToList();
            if (PolicyIdentityValues != null && PolicyIdentityValues.Length > 0)
                values.InsertRange(0, PolicyIdentityValues);

            return values;
        }

        private long GetUnixTimestamp()
        {
            TimeSpan timeSpan = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
            return (long)timeSpan.TotalSeconds;
        }
    }
}
