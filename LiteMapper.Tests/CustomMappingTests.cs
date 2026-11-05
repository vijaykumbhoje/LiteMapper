
using LiteMapper.Configuration;
using LiteMapper.Engine;
using Xunit;

namespace LiteMapper.Tests;

public class CustomMappingTests
{
    public class Employee
    {
        public int Id { get; set; }

        public string FirstName { get; set; } = "";

        public string LastName { get; set; } = "";

        public string Password { get; set; } = "";
    }

    public class EmployeeDto
    {
        public int Id { get; set; }

        public string FullName { get; set; } = "";

        public string Password { get; set; } = "unchanged";
    }

    public class EmployeeProfile : MappingProfile
    {
        public EmployeeProfile()
        {
            CreateMap<Employee, EmployeeDto>()
                .ForMember(
                    dest => dest.FullName,
                    opt => opt.MapFrom(
                        src => $"{src.FirstName} {src.LastName}"))
                .ForMember(
                    dest => dest.Password,
                    opt => opt.Ignore());
        }
    }

    [Fact]
    public void Map_ShouldSupportCustomPropertyMapping()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new EmployeeProfile());
        });

        var mapper = new Mapper(configuration);

        var employee = new Employee
        {
            Id = 101,
            FirstName = "Rahul",
            LastName = "Sharma",
            Password = "secret"
        };

        var result = mapper.Map<EmployeeDto>(employee);

        Assert.Equal(101, result.Id);
        Assert.Equal("Rahul Sharma", result.FullName);
        Assert.Equal("unchanged", result.Password);
    }

    [Fact]
    public void Map_ShouldIgnoreConfiguredProperty()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new EmployeeProfile());
        });

        var mapper = new Mapper(configuration);

        var employee = new Employee
        {
            Password = "secret"
        };

        var existingDto = new EmployeeDto
        {
            Password = "existing"
        };

        mapper.Map(employee, existingDto);

        Assert.Equal("existing", existingDto.Password);
    }

    [Fact]
    public void Map_ShouldHandleSourceWithoutMatchingProperties()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new EmptyProfile());
        });

        var mapper = new Mapper(configuration);

        var source = new SourceWithoutMatchingProperties
        {
            SourceName = "Vijay"
        };

        var result = mapper.Map<
            SourceWithoutMatchingProperties,
            DestinationWithoutMatchingProperties>(source);

        Assert.Equal(0, result.Id);
        Assert.Equal(string.Empty, result.Name);
    }

    public class SourceWithoutMatchingProperties
    {
        public string SourceName { get; set; } = "";
    }

    public class DestinationWithoutMatchingProperties
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class EmptyProfile : MappingProfile
    {
        public EmptyProfile()
        {
            CreateMap<
                SourceWithoutMatchingProperties,
                DestinationWithoutMatchingProperties>();
        }
    }
}
