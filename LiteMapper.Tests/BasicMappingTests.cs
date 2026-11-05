
using LiteMapper.Configuration;
using LiteMapper.Engine;
using Xunit;

namespace LiteMapper.Tests;

public class BasicMappingTests
{
    public class Employee
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
    }

    public class EmployeeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
    }

    public class EmployeeProfile : MappingProfile
    {
        public EmployeeProfile()
        {
            CreateMap<Employee, EmployeeDto>();
        }
    }

    [Fact]
    public void Map_ShouldCopyMatchingProperties()
    {
        // Arrange
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new EmployeeProfile());
        });

        var mapper = new Mapper(configuration);

        var employee = new Employee
        {
            Id = 101,
            Name = "Rahul",
            Department = "IT"
        };

        // Act
        var result = mapper.Map<EmployeeDto>(employee);

        // Assert
        Assert.Equal(101, result.Id);
        Assert.Equal("Rahul", result.Name);
        Assert.Equal("IT", result.Department);
    }

    [Fact]
    public void Map_ShouldMapIntoExistingObject()
    {
        // Arrange
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new EmployeeProfile());
        });

        var mapper = new Mapper(configuration);

        var employee = new Employee
        {
            Id = 101,
            Name = "Rahul",
            Department = "IT"
        };

        var existingDto = new EmployeeDto
        {
            Name = "Old Name"
        };

        // Act
        var result = mapper.Map(employee, existingDto);

        // Assert
        Assert.Same(existingDto, result);
        Assert.Equal("Rahul", result.Name);
        Assert.Equal(101, result.Id);
    }

    [Fact]
    public void Map_ShouldCreateIndependentDestinationsForRepeatedCalls()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new EmployeeProfile());
        });

        var mapper = new Mapper(configuration);

        var source = new Employee
        {
            Id = 101,
            Name = "Vijay",
            Department = "Engineering"
        };

        var first = mapper.Map<EmployeeDto>(source);
        var second = mapper.Map<EmployeeDto>(source);

        Assert.NotSame(first, second);
        Assert.Equal(first.Id, second.Id);
        Assert.Equal(first.Name, second.Name);

        first.Name = "Modified";

        Assert.Equal("Vijay", second.Name);
    }

    [Fact]
    public void Map_ShouldSkipReadOnlyDestinationProperties()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new ReadOnlyProfile());
        });

        var mapper = new Mapper(configuration);

        var source = new ReadOnlySource
        {
            Id = 101,
            Name = "Vijay"
        };

        var result = mapper.Map<ReadOnlySource, ReadOnlyDestination>(source);

        Assert.Equal(101, result.Id);
        Assert.Equal("Default Name", result.Name);
    }

    public class ReadOnlySource
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class ReadOnlyDestination
    {
        public int Id { get; set; }

        public string Name { get; } = "Default Name";
    }

    public class ReadOnlyProfile : MappingProfile
    {
        public ReadOnlyProfile()
        {
            CreateMap<ReadOnlySource, ReadOnlyDestination>();
        }
    }
}
