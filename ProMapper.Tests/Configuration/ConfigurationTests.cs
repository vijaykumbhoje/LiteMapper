using ProMapper.Configuration;
using ProMapper.Engine;
using Xunit;

namespace ProMapper.Tests.Configuration;

public class ConfigurationTests
{

    [Fact]
    public void Configuration_ShouldRejectDuplicateMappings()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            _ = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new EmployeeProfile());
                cfg.AddProfile(new EmployeeProfile());
            });
        });

        Assert.Contains("already registered", exception.Message);
    }

    [Fact]
    public void Configuration_ShouldRejectChangesAfterInitialization()
    {
        MappingExpression<Employee, EmployeeDto>? mapping = null;

        var configuration = new MapperConfiguration(cfg =>
        {
            var profile = new EmployeeProfile();
            cfg.AddProfile(profile);
            mapping = profile.Mapping;
        });

        Assert.NotNull(mapping);

        Assert.Throws<InvalidOperationException>(() =>
        {
            mapping!.ForMember(
                dest => dest.Name,
                opt => opt.Ignore());
        });
    }

    [Fact]
    public void Configuration_ShouldAllowExplicitValidation()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new EmployeeProfile());
        });

        var exception = Record.Exception(
            () => configuration.AssertConfigurationIsValid());

        Assert.Null(exception);
    }

    [Fact]
    public void Map_ShouldRejectUnregisteredMapping()
    {
        var configuration = new MapperConfiguration(_ => { });
        var mapper = new Mapper(configuration);

        Assert.Throws<InvalidOperationException>(() =>
            mapper.Map<UnregisteredSource, UnregisteredDestination>(
                new UnregisteredSource()));
    }

    [Fact]
    public void Map_ShouldHandleConcurrentMappingRequests()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new EmployeeProfile());
        });

        var mapper = new Mapper(configuration);

        // All threads use the same mapper and initially request
        // the same mapping plan.
        Parallel.For(0, 100, i =>
        {
            var source = new Employee
            {
                Id = i,
                Name = $"Employee {i}"
            };

            var result = mapper.Map<Employee, EmployeeDto>(source);

            Assert.Equal(i, result.Id);
            Assert.Equal($"Employee {i}", result.Name);
        });
    }

    private class UnregisteredSource
    {
        public int Id { get; set; }
    }

    private class UnregisteredDestination
    {
        public int Id { get; set; }
    }
    private class Employee
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    private class EmployeeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    private class EmployeeProfile : MappingProfile
    {
        public MappingExpression<Employee, EmployeeDto> Mapping { get; }

        public EmployeeProfile()
        {
            Mapping = CreateMap<Employee, EmployeeDto>();
        }
    }
}