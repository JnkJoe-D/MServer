# 依赖项注入（DI）

- **参考学习：**
- [探索 .NET Core 依赖注入的 IServiceCollection - SpringLeee - 博客园](https://www.cnblogs.com/myshowtime/p/14409907.html)
- [(39 封私信) 探索 .NET Core 依赖注入的 IServiceProvider - 知乎](https://zhuanlan.zhihu.com/p/354003618)

- [ ] hostBuilder.ConfigureServices((hostCtx, services)，这个方法我了解到，服务被添加到IServiceCollection中，然后每个服务注册是由ServiceDescriptor描述的，那么具体ServiceDescriptor是怎么实现？

你问到了依赖注入系统的**心脏部位**。`ServiceDescriptor` 是描述“如何创建一个服务”的**蓝图**或**配方**。下面我们来深入解析它的实现。

### 📐 ServiceDescriptor 的核心结构

`ServiceDescriptor` 本质上是一个**数据传输对象**，它用几个关键属性来完整描述一个服务的注册信息。这是其最简化的概念模型：

```csharp
// 这是Microsoft.Extensions.DependencyInjection.Abstractions中ServiceDescriptor的简化概念模型
public class ServiceDescriptor
{
    // 1. 服务类型 (通常是一个接口或抽象类)
    public Type ServiceType { get; }
    
    // 2. 实现类型 (具体类)、或实现工厂、或实现实例 (三者选其一)
    public Type ImplementationType { get; }
    public Func<IServiceProvider, object> ImplementationFactory { get; }
    public object ImplementationInstance { get; }
    
    // 3. 生命周期
    public ServiceLifetime Lifetime { get; } // 枚举：Singleton, Scoped, Transient
    
    // 关键方法：根据提供的信息，创建不同类型的ServiceDescriptor
    public static ServiceDescriptor Singleton<TService, TImplementation>()
        where TImplementation : TService;
    public static ServiceDescriptor Scoped<TService>(Func<IServiceProvider, TService> factory);
    // ... 其他工厂方法
}
```

### 🧩 三种注册方式在 ServiceDescriptor 中的体现

当你调用 `services.AddXxx()` 时，内部就是在创建不同属性的 `ServiceDescriptor` 并加入集合。下面的表格清晰地展示了它们如何与注册方法对应：

| 你写的注册代码                                               | 对应的 `ServiceDescriptor` 内部状态                          | 容器后续如何创建实例                                         |
| :----------------------------------------------------------- | :----------------------------------------------------------- | :----------------------------------------------------------- |
| **`services.AddSingleton<JwtService>()`**                    | `ServiceType` = `typeof(JwtService)` <br> `ImplementationType` = `typeof(JwtService)` <br> `Lifetime` = `ServiceLifetime.Singleton` <br> `ImplementationFactory` = `null` <br> `ImplementationInstance` = `null` | 容器通过反射，调用 `ImplementationType` 的构造函数来创建实例。 |
| **`services.AddScoped<IMessageHandler, LoginHandler>()`**    | `ServiceType` = `typeof(IMessageHandler)` <br> `ImplementationType` = `typeof(LoginHandler)` <br> `Lifetime` = `ServiceLifetime.Scoped` | 容器通过反射，创建 `LoginHandler` 的实例，但以 `IMessageHandler` 类型返回。 |
| **`services.AddSingleton<RedisService>(sp => new RedisService(...))`** | `ServiceType` = `typeof(RedisService)` <br> `ImplementationFactory` = `sp => new RedisService(...)` <br> `Lifetime` = `ServiceLifetime.Singleton` <br> `ImplementationType` = `null` | 容器直接执行 `ImplementationFactory` 委托来获取实例。        |
| **`services.AddSingleton<ILogger>(existingLogger)`**         | `ServiceType` = `typeof(ILogger)` <br> `ImplementationInstance` = `existingLogger` <br> `Lifetime` = `ServiceLifetime.Singleton` | 容器直接返回已提供的 `ImplementationInstance`。              |

### 🔧 核心实现原理探究

让我们深入到 `Microsoft.Extensions.DependencyInjection` 库的源代码层面，看看关键实现逻辑（基于开源代码的简化）：

**1. `AddSingleton<TService>()` 的内部实现**
```csharp
public static IServiceCollection AddSingleton<TService>(this IServiceCollection services)
{
    // 1. 创建描述符
    var descriptor = ServiceDescriptor.Singleton(typeof(TService), typeof(TService));
    // 2. 添加到集合
    services.Add(descriptor);
    return services;
}

// ServiceDescriptor 的静态工厂方法
public static ServiceDescriptor Singleton(Type serviceType, Type implementationType)
{
    return new ServiceDescriptor(serviceType, implementationType, ServiceLifetime.Singleton);
}

// ServiceDescriptor 构造函数核心逻辑
private ServiceDescriptor(Type serviceType, Type implementationType, ServiceLifetime lifetime)
{
    ServiceType = serviceType;
    ImplementationType = implementationType;
    Lifetime = lifetime;
    // ImplementationFactory 和 ImplementationInstance 为 null
}
```

**2. `AddSingleton<TService>(工厂方法)` 的内部实现**
```csharp
public static IServiceCollection AddSingleton<TService>(
    this IServiceCollection services,
    Func<IServiceProvider, TService> implementationFactory)
{
    // 注意：这里存储的是工厂委托，而不是具体类型
    var descriptor = new ServiceDescriptor(
        typeof(TService), 
        implementationFactory, // 工厂委托作为参数
        ServiceLifetime.Singleton
    );
    services.Add(descriptor);
    return services;
}

// 对应的 ServiceDescriptor 构造函数
private ServiceDescriptor(
    Type serviceType,
    Func<IServiceProvider, object> implementationFactory,
    ServiceLifetime lifetime)
{
    ServiceType = serviceType;
    ImplementationFactory = implementationFactory; // 关键：保存工厂
    Lifetime = lifetime;
    // ImplementationType 为 null
}
```

**3. `IServiceCollection` 的本质**
`IServiceCollection` 本质上只是一个 `ServiceDescriptor` 的集合：
```csharp
// 实际上，IServiceCollection 就是 IList<ServiceDescriptor>
public interface IServiceCollection : IList<ServiceDescriptor> { }

// 默认实现
public class ServiceCollection : IServiceCollection
{
    private readonly List<ServiceDescriptor> _descriptors = new List<ServiceDescriptor>();
    // ... 实现 IList<T> 的所有方法，操作 _descriptors 列表
}
```

### ⚙️ 容器如何消费 ServiceDescriptor

当调用 `BuildServiceProvider()` 时，会发生以下关键转换：

```csharp
// 简化的构建过程
public ServiceProvider BuildServiceProvider()
{
    // 1. 读取所有 ServiceDescriptor
    var descriptors = _services.ToArray(); // _services 就是 IServiceCollection
    
    // 2. 根据描述符创建“服务调用站点”
    // 这是一个复杂的过程，涉及：
    // - 生命周期管理（单例缓存、作用域跟踪）
    // - 构造函数依赖分析
    // - 循环依赖检测
    // - 动态编译或表达式树生成，用于优化实例创建速度
    var callSites = CreateCallSites(descriptors);
    
    // 3. 返回一个能根据这些“站点”快速创建服务的提供程序
    return new ServiceProvider(callSites, ...);
}
```

**关键优化**：在构建时，容器不会立即创建服务实例，而是根据 `ServiceDescriptor` 生成高效的 **“调用站点”** 。对于 `ImplementationType`，可能编译成类似 `new LoginHandler(authService)` 的表达式树；对于 `ImplementationFactory`，则直接包装该委托。这避免了每次解析都使用反射，极大提升了性能。

### 📊 在你的项目中：一个具体的注册链条

以你的 `AuthService` 注册为例，容器内部的依赖分析链如下：

```csharp
// 你的注册
services.AddScoped<AuthService>();

// 容器分析链：
1. 找到 AuthService 的 ServiceDescriptor。
2. 发现其 ImplementationType 是 AuthService，Lifetime 是 Scoped。
3. 分析 AuthService 的构造函数：public AuthService(JwtService jwt, PlayerManager pm)。
4. 递归查找 JwtService 和 PlayerManager 的 ServiceDescriptor。
5. 发现它们都是 Singleton，标记为“可解析”。
6. 在运行时，当第一个 Scoped 请求需要 AuthService 时：
   a. 创建一个新的作用域。
   b. 在该作用域内，调用 JwtService 和 PlayerManager 的单例获取逻辑。
   c. 用这两个实例，通过编译好的工厂方法 new AuthService(jwt, pm) 创建 AuthService 实例。
   d. 将该实例与当前作用域关联，以便在同一作用域内后续请求返回同一实例。
```

### 💎 总结

`ServiceDescriptor` 是依赖注入系统的**契约**或**配方**，它通过 `ServiceType`、实现信息（类型/工厂/实例）、`Lifetime` 这三个核心要素，完整地定义了“如何提供某种服务”。

它的设计精妙之处在于：
1.  **信息完备**：涵盖了所有可能的服务注册方式。
2.  **与容器解耦**：`IServiceCollection` 只负责收集描述符，不负责创建。构建时，容器再将其转换为高效运行时结构。
3.  **支持复杂场景**：通过工厂方法和实例注册，支持参数化构造和外部实例集成。

理解了 `ServiceDescriptor`，你就真正理解了 .NET 依赖注入从“声明”到“实现”的桥梁。这正是 `ConfigureServices` 方法强大和灵活性的根源所在。