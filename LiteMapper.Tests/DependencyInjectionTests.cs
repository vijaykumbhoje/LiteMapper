
using LiteMapper.Abstractions;
using LiteMapper.Configuration;
using LiteMapper.Conversion;
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

    [Fact]
    public void AddLiteMapper_ShouldResolveScopedCustomConverterDependencies()
    {
        var services = new ServiceCollection();

        services.AddScoped<ICurrencyFormatter, CurrencyFormatter>();

        services.AddScoped<
            ITypeConverter<decimal, string>,
            CurrencyConverter>();

        services.AddLiteMapper(cfg =>
        {
            cfg.AddProfile(new CurrencyMappingProfile());

            cfg.AddTypeConverter<
                decimal,
                string,
                CurrencyConverter>();
        });

        using var serviceProvider = services.BuildServiceProvider();

        string firstResult;
        string secondResult;

        // First scope
        using (var scope = serviceProvider.CreateScope())
        {
            var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

            var result = mapper.Map<CurrencySource, CurrencyDestination>(
                new CurrencySource { Amount = 125.50m });

            firstResult = result.Amount;
        }

        // Second scope
        using (var scope = serviceProvider.CreateScope())
        {
            var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

            var result = mapper.Map<CurrencySource, CurrencyDestination>(
                new CurrencySource { Amount = 125.50m });

            secondResult = result.Amount;
        }

        Assert.NotEqual(firstResult, secondResult);
        Assert.Contains("125.50", firstResult);
        Assert.Contains("125.50", secondResult);
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

    public interface ICurrencyFormatter
    {
        string Format(decimal amount);
    }

    public class CurrencySource
    {
        public decimal Amount { get; set; }
    }

    public class CurrencyDestination
    {
        public string Amount { get; set; } = string.Empty;
    }

    public class CurrencyFormatter : ICurrencyFormatter
    {
        private readonly Guid _scopeId = Guid.NewGuid();

        public string Format(decimal amount)
        {
            return $"{amount:F2}|{_scopeId}";
        }
    }

    public class CurrencyConverter : ITypeConverter<decimal, string>
    {
        private readonly ICurrencyFormatter _formatter;

        public CurrencyConverter(ICurrencyFormatter formatter)
        {
            _formatter = formatter;
        }

        public string Convert(decimal source)
        {
            return _formatter.Format(source);
        }
    }

    public class CurrencyMappingProfile : MappingProfile
    {
        public CurrencyMappingProfile()
        {
            CreateMap<CurrencySource, CurrencyDestination>();
        }
    }
}
