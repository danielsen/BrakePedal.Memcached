using NUnit.Framework;
using Should;
using Enyim.Caching;
using NSubstitute;

namespace BrakePedal.Memcached.Tests
{
    [TestFixture]
    public class MemcachedThrottleRepositoryTests
    {
        private IMemcachedClient _memcachedClient;
        private MemcachedThrottleRepository _memcachedThrottleRepository;
        private SimpleThrottleKey _simpleThrottleKey;

        [SetUp]
        public void Setup()
        {
            _memcachedClient = Substitute.For<IMemcachedClient>();
            _memcachedThrottleRepository = new MemcachedThrottleRepository(_memcachedClient);
            _simpleThrottleKey = new SimpleThrottleKey("test", "key");
        }

        [Test]
        public void should_correctly_increment_key()
        {
            Limiter limiter = new Limiter().Limit(1).Over(10);
            var id = _memcachedThrottleRepository.CreateThrottleKey(_simpleThrottleKey, limiter);

            _memcachedClient.Increment(id, 1, 1, limiter.Period).Returns((ulong) 1);

            _memcachedThrottleRepository.AddOrIncrementWithExpiration(_simpleThrottleKey, limiter);

            _memcachedClient.Received(1).Increment(id, 1, 1, limiter.Period);
            _memcachedClient.Received(1).Increment(id, 1, 0, limiter.Period);
        }

        [Test]
        public void should_return_null_for_nonexistent_key()
        {
            Limiter limiter = new Limiter().Limit(1).Over(10);
            var id = _memcachedThrottleRepository.CreateThrottleKey(_simpleThrottleKey, limiter);

            _memcachedClient.Get<string>(id).Returns("foo");

            long? result = _memcachedThrottleRepository.GetThrottleCount(_simpleThrottleKey, limiter);

            result.ShouldEqual(null);
        }

        [Test]
        public void should_return_value_for_key()
        {
            Limiter limiter = new Limiter().Limit(1).Over(10);
            var id = _memcachedThrottleRepository.CreateThrottleKey(_simpleThrottleKey, limiter);

            _memcachedClient.Get<string>(id).Returns("10");

            long? result = _memcachedThrottleRepository.GetThrottleCount(_simpleThrottleKey, limiter);

            result.ShouldEqual(10);
        }

        [Test]
        [TestCase(true, true)]
        [TestCase(false, false)]
        public void should_correctly_identify_lock_status(bool keyExists, bool expected)
        {
            Limiter limiter = new Limiter().Limit(1).Over(10).LockFor(1);
            var id = _memcachedThrottleRepository.CreateThrottleKey(_simpleThrottleKey, limiter);

            _memcachedClient.TryGet(id, out object target).ReturnsForAnyArgs(keyExists);

            bool result = _memcachedThrottleRepository.LockExists(_simpleThrottleKey, limiter);

            result.ShouldEqual(expected);
        }

        [Test]
        public void should_remove_throttle()
        {
            Limiter limiter = new Limiter().Limit(1).Over(10).LockFor(1);
            var id = _memcachedThrottleRepository.CreateThrottleKey(_simpleThrottleKey, limiter);
            
            _memcachedThrottleRepository.RemoveThrottle(_simpleThrottleKey, limiter);

            _memcachedClient.Received(1).Remove(id);
        }

        [Test]
        public void should_set_lock()
        {
            Limiter limiter = new Limiter().Limit(1).Over(10).LockFor(1);
            var id = _memcachedThrottleRepository.CreateLockKey(_simpleThrottleKey, limiter);
            
            _memcachedThrottleRepository.SetLock(_simpleThrottleKey, limiter);

            _memcachedClient.Received(1).Increment(id, 1, 1, limiter.LockDuration.Value);
        }
    }
}
