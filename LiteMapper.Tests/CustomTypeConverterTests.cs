using LiteMapper.Abstractions;
using LiteMapper.Configuration;
using LiteMapper.Conversion;
using LiteMapper.Engine;
using Xunit;

namespace LiteMapper.Tests;

public class CustomTypeConverterTests
{
    [Fact]
    public void Map_ShouldUseCustomTypeConverter()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new DateMappingProfile());

            cfg.AddTypeConverter(
                new StringToDateTimeConverter());
        });

        var mapper = new Mapper(configuration);

        var source = new SourceModel
        {
            Date = "2026-11-05"
        };

        var result = mapper.Map<SourceModel, DestinationModel>(source);

        Assert.Equal(
            new DateTime(2026, 11, 5),
            result.Date);
    }

    [Fact]
    public void Map_ShouldPreferCustomConverterOverBuiltInConversion()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new GuidMappingProfile());

            cfg.AddTypeConverter(
                new StringToGuidConverter());
        });

        var mapper = new Mapper(configuration);

        var source = new GuidSource
        {
            Id = "11111111-1111-1111-1111-111111111111"
        };

        var result = mapper.Map<GuidSource, GuidDestination>(source);

        Assert.Equal(Guid.Empty, result.Id);
    }

    [Fact]
    public void AddTypeConverter_ShouldRejectDuplicateRegistration()
    {
        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = new MapperConfiguration(cfg =>
            {
                cfg.AddTypeConverter(
                    new StringToDateTimeConverter());

                cfg.AddTypeConverter(
                    new StringToDateTimeConverter());
            });
        });
    }

    [Fact]
    public void Map_ShouldUseCustomConverterForNullableDestination()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new NullableDateMappingProfile());

            cfg.AddTypeConverter(
                new StringToDateTimeConverter());
        });

        var mapper = new Mapper(configuration);

        var source = new NullableDateSource
        {
            Date = "2026-11-05"
        };

        var result = mapper.Map<NullableDateSource, NullableDateDestination>(source);

        Assert.Equal(new DateTime(2026, 11, 5), result.Date);
    }

    [Fact]
    public void Map_ShouldUseCustomConverterForCollectionElements()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new CollectionMappingProfile());

            cfg.AddTypeConverter(
                new StringToDateTimeConverter());
        });

        var mapper = new Mapper(configuration);

        var source = new CollectionSource
        {
            Dates = new List<string>
        {
            "2026-11-05",
            "2026-11-06"
        }
        };

        var result = mapper.Map<CollectionSource, CollectionDestination>(source);

        Assert.Equal(2, result.Dates.Count);
        Assert.Equal(new DateTime(2026, 11, 5), result.Dates[0]);
        Assert.Equal(new DateTime(2026, 11, 6), result.Dates[1]);
    }

    [Fact]
    public void AddTypeConverter_ShouldRejectRegistrationAfterConfigurationIsSealed()
    {
        var configuration = new MapperConfiguration(_ => { });

        Assert.Throws<InvalidOperationException>(() =>
            configuration.AddTypeConverter(
                new StringToDateTimeConverter()));
    }

    [Fact]
    public void Map_ShouldPropagateCustomConverterException()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new DateMappingProfile());

            cfg.AddTypeConverter(
                new ThrowingDateTimeConverter());
        });

        var mapper = new Mapper(configuration);

        var source = new SourceModel
        {
            Date = "invalid-date"
        };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            mapper.Map<SourceModel, DestinationModel>(source));

        Assert.Equal("Custom conversion failed.", exception.Message);
    }

    public class NullableDateSource
    {
        public string Date { get; set; } = string.Empty;
    }

    public class NullableDateDestination
    {
        public DateTime? Date { get; set; }
    }

    public class NullableDateMappingProfile : MappingProfile
    {
        public NullableDateMappingProfile()
        {
            CreateMap<NullableDateSource, NullableDateDestination>();
        }
    }

    public class CollectionSource
    {
        public List<string> Dates { get; set; } = new();
    }

    public class CollectionDestination
    {
        public List<DateTime> Dates { get; set; } = new();
    }

    public class CollectionMappingProfile : MappingProfile
    {
        public CollectionMappingProfile()
        {
            CreateMap<CollectionSource, CollectionDestination>();
        }
    }

    public class SourceModel
    {
        public string Date { get; set; } = string.Empty;
    }

    public class DestinationModel
    {
        public DateTime Date { get; set; }
    }

    public class GuidSource
    {
        public string Id { get; set; } = string.Empty;
    }

    public class GuidDestination
    {
        public Guid Id { get; set; }
    }

    public class StringToDateTimeConverter
        : ITypeConverter<string, DateTime>
    {
        public DateTime Convert(string source)
        {
            return DateTime.Parse(source);
        }
    }

    public class StringToGuidConverter
        : ITypeConverter<string, Guid>
    {
        public Guid Convert(string source)
        {
            // Deliberately return a different value to prove
            // the custom converter takes precedence.
            return Guid.Empty;
        }
    }

    public class GuidMappingProfile : MappingProfile
    {
        public GuidMappingProfile()
        {
            CreateMap<GuidSource, GuidDestination>();
        }
    }

    public class DateMappingProfile : MappingProfile
    {
        public DateMappingProfile()
        {
            CreateMap<SourceModel, DestinationModel>();
        }
    }

    public class ThrowingDateTimeConverter
    : ITypeConverter<string, DateTime>
    {
        public DateTime Convert(string source)
        {
            throw new InvalidOperationException(
                "Custom conversion failed.");
        }
    }
}