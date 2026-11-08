# ProMapper.DependencyInjection

Dependency injection integration for ProMapper.

This package provides registration extensions for integrating ProMapper into applications using Microsoft.Extensions.DependencyInjection.

## Installation

Install both packages:

```powershell
dotnet add package ProMapper
dotnet add package ProMapper.DependencyInjection
```

## Registration

Register ProMapper in your application's service collection:

```csharp
using ProMapper.DependencyInjection;

services.AddProMapper(cfg =>
{
    cfg.AddProfile(new EmployeeProfile());
});
```

## Constructor Injection

Inject `IMapper` into your application services:

```csharp
public class EmployeeService
{
    private readonly IMapper _mapper;

    public EmployeeService(IMapper mapper)
    {
        _mapper = mapper;
    }
}
```

## Service Lifetimes

- `MapperConfiguration` is registered as a singleton.
- `IMapper` is registered as scoped.

The scoped mapper lifetime allows custom converters to resolve dependencies from the current service scope.

## Supported Frameworks

- .NET 8
- .NET 10

## Related Package

- [ProMapper](https://www.nuget.org/packages/ProMapper)

## License

MIT License. See the ProMapper repository for the license text.