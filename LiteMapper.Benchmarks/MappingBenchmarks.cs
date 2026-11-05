
using System.Reflection;
using AutoMapper;
using BenchmarkDotNet.Attributes;
using LiteMapper.Configuration;
using LiteMapper.Engine;

using LiteMapperConfig = LiteMapper.Configuration.MapperConfiguration;
using LiteMapperInstance = LiteMapper.Engine.Mapper;

namespace LiteMapper.Benchmarks;

[MemoryDiagnoser]
public class MappingBenchmarks
{
    private Employee _source = null!;

    private LiteMapperInstance _liteMapper = null!;
    private IMapper _autoMapper = null!;

    [GlobalSetup]
    public void Setup()
    {
        _source = new Employee
        {
            Id = 101,
            FirstName = "Vijay",
            LastName = "Kumbhoje",
            Email = "vijay@example.com",
            Age = 40
        };

        var liteConfiguration = new LiteMapperConfig(cfg =>
        {
            cfg.AddProfile(new EmployeeProfile());
        });

        _liteMapper = new LiteMapperInstance(liteConfiguration);

        var autoConfiguration = new AutoMapper.MapperConfiguration(
            cfg => cfg.CreateMap<Employee, EmployeeDto>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        _autoMapper = autoConfiguration.CreateMapper();
    }

    [Benchmark(Baseline = true)]
    public EmployeeDto ManualMapping()
    {
        return new EmployeeDto
        {
            Id = _source.Id,
            FirstName = _source.FirstName,
            LastName = _source.LastName,
            Email = _source.Email,
            Age = _source.Age
        };
    }

    [Benchmark]
    public EmployeeDto ReflectionMapping()
    {
        var destination = new EmployeeDto();

        var sourceProperties = typeof(Employee).GetProperties(
            BindingFlags.Instance | BindingFlags.Public);

        var destinationProperties = typeof(EmployeeDto).GetProperties(
            BindingFlags.Instance | BindingFlags.Public);

        foreach (var destinationProperty in destinationProperties)
        {
            var sourceProperty = sourceProperties.FirstOrDefault(
                p => string.Equals(
                    p.Name,
                    destinationProperty.Name,
                    StringComparison.OrdinalIgnoreCase));

            if (sourceProperty == null ||
                !destinationProperty.PropertyType.IsAssignableFrom(
                    sourceProperty.PropertyType))
            {
                continue;
            }

            var value = sourceProperty.GetValue(_source);
            destinationProperty.SetValue(destination, value);
        }

        return destination;
    }

    [Benchmark]
    public EmployeeDto LiteMapperMapping()
    {
        return _liteMapper.Map<Employee, EmployeeDto>(_source);
    }

    [Benchmark]
    public EmployeeDto AutoMapperMapping()
    {
        return _autoMapper.Map<EmployeeDto>(_source);
    }

    public class Employee
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public int Age { get; set; }
    }

    public class EmployeeDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public int Age { get; set; }
    }

    public class EmployeeProfile : MappingProfile
    {
        public EmployeeProfile()
        {
            CreateMap<Employee, EmployeeDto>();
        }
    }
}
