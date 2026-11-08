
using ProMapper.Configuration;
using ProMapper.Conversion;
using Xunit;

namespace ProMapper.Tests.Configuration;

public class ConfigurationImmutabilityTests
{
    [Fact]
    public void Configuration_ShouldRejectProfileRegistrationAfterInitialization()
    {
        var configuration = new MapperConfiguration(_ => { });

        Assert.Throws<InvalidOperationException>(
            () => configuration.AddProfile(new TestMappingProfile()));
    }

    [Fact]
    public void Configuration_ShouldRejectConverterRegistrationAfterInitialization()
    {
        var configuration = new MapperConfiguration(_ => { });

        Assert.Throws<InvalidOperationException>(
            () => configuration.AddTypeConverter(
                new IntegerToStringConverter()));
    }

    [Fact]
    public void Configuration_ShouldRejectConverterTypeRegistrationAfterInitialization()
    {
        var configuration = new MapperConfiguration(_ => { });

        Assert.Throws<InvalidOperationException>(
            () => configuration.AddTypeConverter<
                int, string, IntegerToStringConverter>());
    }

    [Fact]
    public void Configuration_ShouldPreserveExistingMappingAfterInitialization()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new TestMappingProfile());
        });

        var mapper = new ProMapper.Engine.Mapper(configuration);

        var result = mapper.Map<Source, Destination>(
            new Source { Name = "Original" });

        Assert.Equal("Original", result.Name);
    }

    public class Source
    {
        public string Name { get; set; } = string.Empty;
    }

    public class Destination
    {
        public string Name { get; set; } = string.Empty;
    }

    public class TestMappingProfile : MappingProfile
    {
        public TestMappingProfile()
        {
            CreateMap<Source, Destination>();
        }
    }

    public class IntegerToStringConverter : ITypeConverter<int, string>
    {
        public string Convert(int source) => source.ToString();
    }
}
