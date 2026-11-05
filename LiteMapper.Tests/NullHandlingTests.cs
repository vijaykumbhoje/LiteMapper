
using LiteMapper.Configuration;
using LiteMapper.Engine;
using Xunit;

namespace LiteMapper.Tests;

public class NullHandlingTests
{
    private readonly Mapper _mapper;

    public NullHandlingTests()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new NullHandlingProfile());
        });

        _mapper = new Mapper(configuration);
    }

    [Fact]
    public void Map_WithNullSourceObject_ShouldThrowArgumentNullException()
    {
        Employee? source = null;

        Assert.Throws<ArgumentNullException>(
            () => _mapper.Map<EmployeeDto>((object)source!));
    }

    [Fact]
    public void Map_WithNullSourceUsingGenericTypes_ShouldThrowArgumentNullException()
    {
        Employee? source = null;

        Assert.Throws<ArgumentNullException>(
            () => _mapper.Map<Employee, EmployeeDto>(source!));
    }

    [Fact]
    public void Map_WithNullSourceAndExistingDestination_ShouldThrowArgumentNullException()
    {
        Employee? source = null;
        var destination = new EmployeeDto { Id = 100 };

        Assert.Throws<ArgumentNullException>(
            () => _mapper.Map<Employee, EmployeeDto>(
                source!, destination));
    }

    [Fact]
    public void Map_WithNullReferenceProperty_ShouldSetDestinationPropertyToNull()
    {
        var source = new Employee
        {
            Name = null
        };

        var result = _mapper.Map<Employee, EmployeeDto>(source);

        Assert.Null(result.Name);
    }

    [Fact]
    public void Map_WithNullNullableInteger_ShouldSetDestinationToNull()
    {
        var source = new Employee
        {
            Age = null
        };

        var result = _mapper.Map<Employee, EmployeeDto>(source);

        Assert.Null(result.Age);
    }

    [Fact]
    public void Map_WithNullSourceValueToNonNullableDestination_ShouldPreserveExistingValue()
    {
        var source = new Employee
        {
            Score = null
        };

        var destination = new EmployeeDto
        {
            Score = 100
        };

        var result = _mapper.Map<Employee, EmployeeDto>(
            source, destination);

        Assert.Equal(100, result.Score);
    }

    [Fact]
    public void Map_WithNullCollection_ShouldSetDestinationCollectionToNull()
    {
        var source = new Employee
        {
            Skills = null
        };

        var result = _mapper.Map<Employee, EmployeeDto>(source);

        Assert.Null(result.Skills);
    }

    [Fact]
    public void Map_WithEmptyCollection_ShouldReturnEmptyCollection()
    {
        var source = new Employee
        {
            Skills = new List<string>()
        };

        var result = _mapper.Map<Employee, EmployeeDto>(source);

        Assert.NotNull(result.Skills);
        Assert.Empty(result.Skills);
    }

    public class Employee
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public int? Age { get; set; }

        public int? Score { get; set; }

        public List<string>? Skills { get; set; }
    }

    public class EmployeeDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }

        public int? Age { get; set; }

        public int Score { get; set; }

        public List<string>? Skills { get; set; }
    }

    public class NullHandlingProfile : MappingProfile
    {
        public NullHandlingProfile()
        {
            CreateMap<Employee, EmployeeDto>();
        }
    }
}
