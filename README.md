# BrakePedal.Memcached

`BrakePedal.Memcached` is an extension to the [BrakePedal](https://github.com/gopangea/BrakePedal) throttling and rate-limiting library. `BrakePedal.Memcached` adds support for [Memcached](https://memcached.org) backed throttling.

The core library provides the following features:

- Throttling: limit `X attempts` over `Y time period`.
- Locking: after `X attempts` over `Y time period` block future attempts for `Z time period`.

### Packages

Current Version: `1.0.0`

Target Framework: `.NET 4.5` and up.

- `BrakePedal.Memcached` contains an implementation of a Memcached throttle repository which uses [EnyimMemcached](https://github.com/enyim/EnyimMemcached)
    - [nuget.org/packages/BrakePedal.Memcached/](https://www.nuget.org/packages/BrakePedal.Memcached/)

Dependencies:

- [BrakePedal](https://github.com/gopangea/BrakePedal)
    - [nuget.org/packages/BrakePedal](https://www.nuget.org/packages/BrakePedal)
- [EnyimMemcached](https://github.com/enyim/EnyimMemcached)
    - [nuget.org/packages/EnyimMemcached/](https://www.nuget.org/packages/EnyimMemcached/)

### Usage

1. Begin with a Memcached client and repository:

    MemcachedClient client = new MemcachedClient();
    var repository = new MemcachedThrottleRepository(client);

2. Configure a policy:

    var loginPolicy = new ThrottlePolicy(repository)
    {
        Name = "LoginAttempts",
        Prefixes = new[] {"login:attempts"},
        Limiters = new Limiter[]
        {
            new Limiter().Limit(3).Over(TimeSpan.FromSeconds(10)) 
        }
    };

Once the policies have been defined they can be used as follows:

1. Create a key that can unique identify the requester.

    var key = new SimpleThrottleKey("username");

2. Check the policy:

    var check = loginPolicy.Check(key); // NOTE: by default, calling the check method will increment the counter.
                                        // If you want to check the status of a policy but not increment the counter
                                        // pass in false to the increment parameter as follows.
                                        // loginPolicy.Check(key, increment = false); 
        
    if (check.IsThrottled)
    {
        throw new Exception($"Requests throttled. Maximum allowed { check.Limiter.Count } per { check.Limiter.Period }.");
    }

### References

For more information consult the documentation for [BrakePedal](https://github.com/gopangea/BrakePedal) and [EnyimMemcached](https://github.com/enyim/EnyimMemcached).
