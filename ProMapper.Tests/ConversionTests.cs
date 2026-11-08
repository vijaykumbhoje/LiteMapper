using ProMapper.Configuration;
using ProMapper.Engine;
using Xunit;

namespace ProMapper.Tests;

public class ConversionTests
{
    [Fact]
    public void Map_ShouldConvertIntToString()
    {
        var mapper = CreateMapper();

        var result = mapper.Map<Employee, EmployeeDto>(
            new Employee
            {
                Id = 123,
                AgeText = "40"
            });

        Assert.Equal("123", result.EmployeeCode);
    }

    [Fact]
    public void Map_ShouldConvertStringToInt()
    {
        var mapper = CreateMapper();

        var result = mapper.Map<Employee, EmployeeDto>(
            new Employee
            {
                Id = 123,
                AgeText = "40"
            });

        Assert.Equal(40, result.Age);
    }

    [Fact]
    public void Map_ShouldMapIntToNullableInt()
    {
        var mapper = CreateMapper();

        var result = mapper.Map<Employee, EmployeeDto>(
            new Employee
            {
                Id = 123,
                AgeText = "40"
            });

        Assert.Equal(123, result.NullableId);
    }


    [Fact]
    public void Map_ShouldPreserveDestinationWhenNullMapsToNonNullable()
    {
        var mapper = CreateMapper();

        var destination = new EmployeeDto { NullAge = 25 };

        mapper.Map<Employee, EmployeeDto>(
            new Employee { AgeValue = null },
            destination);

        Assert.Equal(25, destination.NullAge);
    }

    private static Mapper CreateMapper()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new EmployeeProfile());
        });

        return new Mapper(configuration);
    }

    public class Employee
    {
        public int Id { get; set; }
        public string AgeText { get; set; } = "0";
        public int? AgeValue { get; set; }
    }

    public class EmployeeDto
    {
        public int NullAge { get; set; }
        public string EmployeeCode { get; set; } = "";
        public int Age { get; set; }
        public int? NullableId { get; set; }
    }

    public class EmployeeProfile : MappingProfile
    {
        public EmployeeProfile()
        {
            CreateMap<Employee, EmployeeDto>()
                .ForMember(
                    dest => dest.NullAge,
                    opt => opt.MapFrom(src => src.AgeValue))
                .ForMember(
                    dest => dest.EmployeeCode,
                    opt => opt.MapFrom(src => src.Id))
                .ForMember(
                    dest => dest.Age,
                    opt => opt.MapFrom(src => src.AgeText))
                .ForMember(
                    dest => dest.NullableId,
                    opt => opt.MapFrom(src => src.Id));
        }
    }


    [Fact]
    public void Map_EnumToString_ShouldConvertEnumName()
    {
        var mapper = CreateEnumMapper();

        var result = mapper.Map<EnumSource, EnumStringDestination>(
            new EnumSource { Status = EmployeeStatus.Active });

        Assert.Equal("Active", result.Status);
    }

    [Fact]
    public void Map_StringToEnum_ShouldConvertEnumValue()
    {
        var mapper = CreateEnumMapper();

        var result = mapper.Map<EnumStringSource, EnumValueDestination>(
            new EnumStringSource { Status = "Active" });

        Assert.Equal(EmployeeStatus.Active, result.Status);
    }

    [Fact]
    public void Map_ShouldThrowFormatExceptionForInvalidNumericString()
    {
        var mapper = CreateErrorHandlingMapper();

        var source = new InvalidNumberSource
        {
            Value = "abc"
        };

        Assert.Throws<FormatException>(() =>
            mapper.Map<InvalidNumberSource, InvalidNumberDestination>(source));
    }

    [Fact]
    public void Map_ShouldThrowInvalidOperationExceptionForUnsupportedConversion()
    {
        var mapper = CreateErrorHandlingMapper();

        var source = new UnsupportedSource
        {
            Value = new UnsupportedValue()
        };

        Assert.Throws<InvalidOperationException>(() =>
            mapper.Map<UnsupportedSource, UnsupportedDestination>(source));
    }

    private static Mapper CreateEnumMapper()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new EnumProfile());
        });

        return new Mapper(configuration);
    }

    private static Mapper CreateErrorHandlingMapper()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new ErrorHandlingProfile());
        });

        return new Mapper(configuration);
    }

    public class InvalidNumberSource
    {
        public string Value { get; set; } = "";
    }

    public class InvalidNumberDestination
    {
        public int Value { get; set; }
    }

    public class UnsupportedValue
    {
    }

    public class UnsupportedSource
    {
        public UnsupportedValue Value { get; set; } = new();
    }

    public class UnsupportedDestination
    {
        public int Value { get; set; }
    }

    public class ErrorHandlingProfile : MappingProfile
    {
        public ErrorHandlingProfile()
        {
            CreateMap<InvalidNumberSource, InvalidNumberDestination>();
            CreateMap<UnsupportedSource, UnsupportedDestination>();
        }
    }

    public enum EmployeeStatus
    {
        Pending = 0,
        Active = 1,
        Inactive = 2
    }

    public class EnumSource
    {
        public EmployeeStatus Status { get; set; }
    }

    public class EnumStringSource
    {
        public string Status { get; set; } = "";
    }

    public class EnumDestination
    {
        public EmployeeStatus Status { get; set; }
    }

    public class EnumStringDestination
    {
        public string Status { get; set; } = "";
    }

    public class EnumValueDestination
    {
        public EmployeeStatus Status { get; set; }
    }

    public class EnumProfile : MappingProfile
    {
        public EnumProfile()
        {
            CreateMap<EnumSource, EnumStringDestination>();
            CreateMap<EnumStringSource, EnumValueDestination>();
        }
    }

}