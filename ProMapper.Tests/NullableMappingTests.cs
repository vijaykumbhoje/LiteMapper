
using ProMapper.Configuration;
using ProMapper.Engine;
using Xunit;

namespace ProMapper.Tests;

public class NullableMappingTests
{
    private readonly Mapper _mapper;

    public NullableMappingTests()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new NullableProfile());
        });

        _mapper = new Mapper(configuration);
    }

    [Fact]
    public void Map_IntToNullableInt_ShouldPreserveValue()
    {
        var result = _mapper.Map<Source, Destination>(
            new Source { Age = 40 });

        Assert.Equal(40, result.Age);
    }

    [Fact]
    public void Map_NullableIntToNullableInt_ShouldPreserveValue()
    {
        var result = _mapper.Map<Source, Destination>(
            new Source { Age = 40 });

        Assert.Equal(40, result.Age);
    }

    [Fact]
    public void Map_NullNullableIntToNullableInt_ShouldReturnNull()
    {
        var result = _mapper.Map<Source, Destination>(
            new Source { Age = null });

        Assert.Null(result.Age);
    }

    [Fact]
    public void Map_NullableIntToNonNullableInt_ShouldPreserveExistingDestinationValue()
    {
        var destination = new Destination { RequiredAge = 25 };

        var result = _mapper.Map<Source, Destination>(
            new Source { Age = null },
            destination);

        Assert.Equal(25, result.RequiredAge);
    }

    [Fact]
    public void Map_NullableIntToNonNullableInt_ShouldUseDefaultForNewDestination()
    {
        var result = _mapper.Map<Source, Destination>(
            new Source { Age = null });

        Assert.Equal(0, result.RequiredAge);
    }

    public class Source
    {
        public int? Age { get; set; }
    }

    public class Destination
    {
        public int? Age { get; set; }

        public int RequiredAge { get; set; }
    }

    public class NullableProfile : MappingProfile
    {
        public NullableProfile()
        {
            CreateMap<Source, Destination>();
        }
    }
}
