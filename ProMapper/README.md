# ProMapper

**A lightweight, extensible object-to-object mapping library for .NET.**

ProMapper is a .NET mapping library designed to simplify transferring data between objects, DTOs, and application models.

It provides convention-based mapping, configurable member mappings, custom type converters, nested object mapping, collection support, circular reference handling, and dependency injection integration.

Built with performance and maintainability in mind, ProMapper uses cached mapping plans and compiled property accessors to reduce repeated mapping overhead.

[![NuGet](https://img.shields.io/nuget/v/ProMapper.svg)](https://www.nuget.org/packages/ProMapper)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ProMapper.svg)](https://www.nuget.org/packages/ProMapper)

---

## Features

- **Convention-based mapping** – Automatically maps matching source and destination properties.
- **Custom member mapping** – Configure specific destination members using custom source expressions.
- **Ignore members** – Exclude selected destination properties from mapping.
- **Nested object mapping** – Map nested objects using registered mapping configurations.
- **Collection mapping** – Supports common collection types, including:
  - `List<T>`
  - Arrays
  - `IEnumerable<T>`
  - `ICollection<T>`
  - `IReadOnlyCollection<T>`
  - `IReadOnlyList<T>`
  - `HashSet<T>`
  - `ISet<T>`
  - `Queue<T>`
  - `Stack<T>`
- **Existing destination mapping** – Map into an existing destination object and reuse compatible mutable collections.
- **Circular reference handling** – Tracks mapped object references to preserve object graph relationships.
- **Maximum mapping depth** – Protects against excessively deep object graphs.
- **Custom type converters** – Register custom conversion logic for specific source and destination types.
- **Dependency injection** – Integrates with Microsoft dependency injection through a separate package.
- **Cached mapping plans** – Reuses generated mapping plans for repeated mapping operations.
- **Compiled property accessors** – Uses compiled expressions for property access.
- **Thread-safe plan caching** – Uses concurrent caching for mapping plan creation.

## Supported Frameworks

| Framework | Support |
|---|---|
| .NET 8 | Supported |
| .NET 10 | Supported |

## Installation

### Core package

Install the ProMapper package:

```powershell
dotnet add package ProMapper
```

Or use the NuGet Package Manager:

```powershell
Install-Package ProMapper
```

### Dependency injection package

For applications using Microsoft.Extensions.DependencyInjection:

```powershell
dotnet add package ProMapper.DependencyInjection
```

## Quick Start

### 1. Create source and destination classes

```csharp
public class Employee
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}

public class EmployeeDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}
```

### 2. Create a mapping profile

```csharp
using ProMapper.Configuration;

public class EmployeeProfile : Profile
{
    public EmployeeProfile()
    {
        CreateMap<Employee, EmployeeDto>();
    }
}
```

### 3. Configure ProMapper

```csharp
using ProMapper;

var configuration = new MapperConfiguration(cfg =>
{
    cfg.AddProfile(new EmployeeProfile());
});

var mapper = new Mapper(configuration);
```

### 4. Map an object

```csharp
var employee = new Employee
{
    Id = 101,
    Name = "John",
    Email = "john@example.com"
};

var employeeDto = mapper.Map<Employee, EmployeeDto>(employee);
```

ProMapper automatically maps matching properties by convention.

## Mapping into an Existing Destination

ProMapper also supports mapping into an existing destination object.

```csharp
var employeeDto = new EmployeeDto();

mapper.Map(employee, employeeDto);
```

This allows you to update an existing destination instance instead of creating a new one.

## Custom Member Mapping

You can configure specific destination properties using mapping expressions.

For example, map a source property to a differently named destination property:

```csharp
public class Employee
{
    public string FirstName { get; set; } = string.Empty;
}

public class EmployeeDto
{
    public string DisplayName { get; set; } = string.Empty;
}
```

Configure the mapping using your profile's `ForMember` and `MapFrom` configuration:

```csharp
public class EmployeeProfile : Profile
{
    public EmployeeProfile()
    {
        CreateMap<Employee, EmployeeDto>()
            .ForMember(
                destination => destination.DisplayName,
                options => options.MapFrom(source => source.FirstName));
    }
}
```

Use the actual member configuration overload supported by your ProMapper version.

## Ignoring Properties

Destination properties can be excluded from mapping through member configuration.

This is useful when a destination property should retain its existing value or be populated separately.

```csharp
// Example configuration concept:
// Ignore the destination property using ForMember and Ignore.
```

See the examples in the repository for the exact supported configuration syntax.

## Nested Object Mapping

ProMapper supports mapping nested objects when the corresponding source-to-destination mapping is registered.

Example:

```csharp
public class Department
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class DepartmentDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class Employee
{
    public int Id { get; set; }

    public Department? Department { get; set; }
}

public class EmployeeDto
{
    public int Id { get; set; }

    public DepartmentDto? Department { get; set; }
}
```

Register both mappings:

```csharp
public class EmployeeProfile : Profile
{
    public EmployeeProfile()
    {
        CreateMap<Department, DepartmentDto>();
        CreateMap<Employee, EmployeeDto>();
    }
}
```

## Collection Mapping

ProMapper supports mapping common collection types and mapping their elements using registered element mappings.

Example:

```csharp
public class Employee
{
    public List<EmployeeItem> Employees { get; set; } = new();
}

public class EmployeeDto
{
    public List<EmployeeItemDto> Employees { get; set; } = new();
}
```

Register the element and parent mappings:

```csharp
public class EmployeeProfile : Profile
{
    public EmployeeProfile()
    {
        CreateMap<EmployeeItem, EmployeeItemDto>();
        CreateMap<Employee, EmployeeDto>();
    }
}
```

ProMapper supports collection scenarios including:

- Collection-to-collection mapping.
- Collection-to-array mapping.
- Collection interface destinations.
- HashSet and ISet destinations.
- Queue and Stack destinations.
- Existing mutable collection reuse.
- Getter-only mutable collection population.
- Null and empty collection handling.

For `Stack<T>`, enumeration follows the normal .NET LIFO behavior.

## Custom Type Converters

ProMapper supports custom type converters for application-specific conversion requirements.

A converter can encapsulate conversion logic that cannot be handled by ordinary property mapping or built-in value conversion.

Use the `ITypeConverter<TSource, TDestination>` contract and register the converter through `MapperConfiguration`.

Refer to the repository examples for the exact converter registration overloads.

## Dependency Injection

Install the additional package:

```powershell
dotnet add package ProMapper.DependencyInjection
```

ProMapper provides dependency injection integration through its `AddProMapper` registration extension.

Example usage pattern:

```csharp
// Register ProMapper using the configuration
// overload provided by ProMapper.DependencyInjection.

// Resolve IMapper through constructor injection.
public class EmployeeService
{
    private readonly IMapper _mapper;

    public EmployeeService(IMapper mapper)
    {
        _mapper = mapper;
    }
}
```

The mapper is registered with a scoped lifetime to support scoped converter dependencies. Mapping configuration is shared as a singleton.

See the dependency injection project examples for the exact registration overload.

## Configuration Validation

ProMapper supports configuration validation.

Use `AssertConfigurationIsValid()` on your `MapperConfiguration` to validate registered mappings before using them.

Example:

```csharp
var configuration = new MapperConfiguration(cfg =>
{
    cfg.AddProfile(new EmployeeProfile());
});

configuration.AssertConfigurationIsValid();
```

## Performance

ProMapper uses several techniques to reduce repeated mapping overhead:

- Cached mapping plans.
- Concurrent mapping plan cache.
- Lazy mapping plan initialization.
- Compiled source property getters.
- Compiled destination property getters and setters.
- Reusable mapping configuration.

Performance depends on the source and destination types, object graph complexity, conversion requirements, runtime, and hardware.

Benchmark comparisons should be performed using the same environment and workload. See the benchmark project in the repository for measured results.

## Thread Safety

Mapping configuration is sealed after initialization.

Mapping plans are cached using a concurrent dictionary and lazy initialization.

Mapping contexts are created per mapping operation when reference tracking is required.

Custom converter instances supplied by applications should be thread-safe if they are shared across concurrent mapping operations.

## Limitations and Design Notes

- Mapping definitions must be registered before mapping.
- Configuration cannot be modified after initialization.
- Nested object mappings require the corresponding mapping definitions.
- Custom converters are required for conversion scenarios not supported by built-in conversion logic.
- Native AOT and source-generator support are not currently advertised as supported features.

## Packages

| Package | Description |
|---|---|
| `ProMapper` | Core object mapping functionality |
| `ProMapper.DependencyInjection` | Microsoft dependency injection integration |

## Contributing

Contributions, bug reports, and feature suggestions are welcome.

Please open an issue or submit a pull request through the project repository.

## License

ProMapper is distributed under the license included in the repository.

See the `LICENSE` file for details.

---

**ProMapper – Simple, extensible object mapping for .NET.**