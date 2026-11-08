
using ProMapper.Abstractions;
using ProMapper.Configuration;
using ProMapper.Conversion;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ProMapper.DependencyInjection;

namespace ProMapper.Tests;

public class DependencyInjectionTests
{
    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddProMapper(cfg =>
        {
            cfg.AddProfile(new EmployeeProfile());
        });

        return services.BuildServiceProvider();
    }

    [Fact]
    public void AddProMapper_ShouldRegisterIMapper()
    {
        using var provider = CreateServiceProvider();

        var mapper = provider.GetService<IMapper>();

        Assert.NotNull(mapper);
    }

    [Fact]
    public void AddProMapper_ShouldRegisterMapperConfiguration()
    {
        using var provider = CreateServiceProvider();

        var configuration =
            provider.GetService<MapperConfiguration>();

        Assert.NotNull(configuration);
        configuration.AssertConfigurationIsValid();
    }

    [Fact]
    public void AddProMapper_ShouldMapObjectsThroughDependencyInjection()
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
    public void AddProMapper_ShouldRegisterMapperAsScoped()
    {
        using var provider = CreateServiceProvider();

        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();

        var first = firstScope.ServiceProvider.GetRequiredService<IMapper>();
        var second = secondScope.ServiceProvider.GetRequiredService<IMapper>();

        Assert.Same(
            first,
            firstScope.ServiceProvider.GetRequiredService<IMapper>());

        Assert.NotSame(first, second);
    }

    [Fact]
    public void AddProMapper_ShouldRegisterConfigurationAsSingleton()
    {
        using var provider = CreateServiceProvider();

        var first = provider.GetRequiredService<MapperConfiguration>();
        var second = provider.GetRequiredService<MapperConfiguration>();

        Assert.Same(first, second);
    }

    [Fact]
    public void AddProMapper_ShouldResolveScopedCustomConverterDependencies()
    {
        var services = new ServiceCollection();

        services.AddScoped<ICurrencyFormatter, CurrencyFormatter>();

        services.AddScoped<
            ITypeConverter<decimal, string>,
            CurrencyConverter>();

        services.AddProMapper(cfg =>
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

    [Fact]
    public async Task AddProMapper_ShouldIsolateScopedConvertersAcrossConcurrentScopes()
    {
        var services = new ServiceCollection();

        services.AddScoped<ICurrencyFormatter, CurrencyFormatter>();

        services.AddScoped<
            ITypeConverter<decimal, string>,
            CurrencyConverter>();

        services.AddProMapper(cfg =>
        {
            cfg.AddProfile(new CurrencyMappingProfile());

            cfg.AddTypeConverter<
                decimal,
                string,
                CurrencyConverter>();
        });

        using var serviceProvider = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateScopes = true,
                ValidateOnBuild = true
            });

        var tasks = Enumerable.Range(1, 100)
            .Select(_ => Task.Run(() =>
            {
                using var scope = serviceProvider.CreateScope();

                var mapper =
                    scope.ServiceProvider.GetRequiredService<IMapper>();

                var result = mapper.Map<
                    CurrencySource,
                    CurrencyDestination>(
                    new CurrencySource { Amount = 125.50m });

                return result.Amount;
            }));

        var results = await Task.WhenAll(tasks);

        Assert.Equal(100, results.Length);

        // Every scope should have its own scoped formatter instance.
        var scopeIds = results
            .Select(result => result.Split('|')[1])
            .ToList();

        Assert.Equal(100, scopeIds.Distinct().Count());

        // Every converter should still receive the correct amount.
        Assert.All(results, result =>
        {
            Assert.StartsWith("125.50|", result);
        });
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

    public class IntegerSource
    {
        public int Amount { get; set; }
    }

    public class IntegerDestination
    {
        public string Amount { get; set; } = string.Empty;
    }

    public class DecimalSource
    {
        public decimal Amount { get; set; }
    }

    public class DecimalDestination
    {
        public string Amount { get; set; } = string.Empty;
    }

    public class IntegerToStringConverter : ITypeConverter<int, string>
    {
        public string Convert(int source) => $"INT-{source}";
    }

    public class DecimalToStringConverter : ITypeConverter<decimal, string>
    {
        public string Convert(decimal source) => $"DEC-{source:F2}";
    }

    public class MultiConverterMappingProfile : MappingProfile
    {
        public MultiConverterMappingProfile()
        {
            CreateMap<IntegerSource, IntegerDestination>();
            CreateMap<DecimalSource, DecimalDestination>();
        }
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
