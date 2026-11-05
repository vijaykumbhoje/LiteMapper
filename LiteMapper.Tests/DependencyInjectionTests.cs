
using LiteMapper.Abstractions;
using LiteMapper.Configuration;
using LiteMapper.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LiteMapper.Tests;

public class DependencyInjectionTests
{
    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddLiteMapper(cfg =>
        {
            cfg.AddProfile(new EmployeeProfile());
        });

        return services.BuildServiceProvider();
    }

    [Fact]
    public void AddLiteMapper_ShouldRegisterIMapper()
    {
        using var provider = CreateServiceProvider();

        var mapper = provider.GetService<IMapper>();

        Assert.NotNull(mapper);
    }

    [Fact]
    public void AddLiteMapper_ShouldRegisterMapperConfiguration()
    {
        using var provider = CreateServiceProvider();

        var configuration =
            provider.GetService<MapperConfiguration>();

        Assert.NotNull(configuration);
        configuration.AssertConfigurationIsValid();
    }

    [Fact]
    public void AddLiteMapper_ShouldMapObjectsThroughDependencyInjection()
    {
        using var provider = CreateServiceProvider();

        var mapper = provider.GetRequiredService<IMapper>();

        var employee = new Employee
        {
            Id = 101,
            Name = "Vijay"
        };

        var result = mapper.Map<Employee, EmployeeDto>(employee);

        Assert.Equal(101, result.Id);
        Assert.Equal("Vijay", result.Name);
    }

    [Fact]
    public void AddLiteMapper_ShouldRegisterMapperAsSingleton()
    {
        using var provider = CreateServiceProvider();

        var first = provider.GetRequiredService<IMapper>();
        var second = provider.GetRequiredService<IMapper>();

        Assert.Same(first, second);
    }

    [Fact]
    public void AddLiteMapper_ShouldRegisterConfigurationAsSingleton()
    {
        using var provider = CreateServiceProvider();

        var first = provider.GetRequiredService<MapperConfiguration>();
        var second = provider.GetRequiredService<MapperConfiguration>();

        Assert.Same(first, second);
    }

    public class Employee
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";
    }

    public class EmployeeDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";
    }

    public class EmployeeProfile : MappingProfile
    {
        public EmployeeProfile()
        {
            CreateMap<Employee, EmployeeDto>();
        }
    }
}
