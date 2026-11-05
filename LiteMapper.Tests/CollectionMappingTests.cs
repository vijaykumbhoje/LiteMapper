using LiteMapper.Configuration;
using LiteMapper.Engine;
using Xunit;

namespace LiteMapper.Tests;

public class CollectionMappingTests
{
    [Fact]
    public void Map_ShouldMapListOfObjects()
    {
        var mapper = CreateMapper();

        var employee = new Employee
        {
            Id = 1,
            Employees =
            [
                new EmployeeItem { Id = 101, Name = "Vijay" },
            new EmployeeItem { Id = 102, Name = "Rahul" }
            ]
        };

        var result = mapper.Map<Employee, EmployeeDto>(employee);

        Assert.Equal(1, result.Id);
        Assert.Equal(2, result.Employees!.Count);
        Assert.Equal("Vijay", result.Employees[0].Name);
        Assert.Equal("Rahul", result.Employees[1].Name);
    }

    [Fact]
    public void Map_ShouldMapArrayOfObjects()
    {
        var mapper = CreateMapper();

        var source = new EmployeeArray
        {
            Employees =
            [
                new EmployeeItem { Id = 101, Name = "Vijay" },
                new EmployeeItem { Id = 102, Name = "Rahul" }
            ]
        };

        var result = mapper.Map<EmployeeArray, EmployeeArrayDto>(source);

        Assert.Equal(2, result.Employees.Length);
        Assert.Equal("Vijay", result.Employees[0].Name);
    }

    [Fact]
    public void Map_ShouldConvertPrimitiveCollection()
    {
        var mapper = CreateMapper();

        var source = new NumberCollection
        {
            Values = ["10", "20", "30"]
        };

        var result = mapper.Map<NumberCollection, NumberCollectionDto>(source);

        Assert.Equal([10, 20, 30], result.Values);
    }

    [Fact]
    public void Map_ShouldHandleNullCollection()
    {
        var mapper = CreateMapper();

        var source = new Employee
        {
            Employees = null
        };

        var result = mapper.Map<Employee, EmployeeDto>(source);

        Assert.Null(result.Employees);
    }


    [Fact]
    public void Map_ShouldMapEmptyCollection()
    {
        var mapper = CreateMapper();

        var source = new Employee
        {
            Employees = []
        };

        var result = mapper.Map<Employee, EmployeeDto>(source);

        Assert.NotNull(result.Employees);
        Assert.Empty(result.Employees);
    }

    [Fact]
    public void Map_ShouldMapCollectionContainingNullElements()
    {
        var mapper = CreateMapper();

        var source = new Employee
        {
            Employees =
            [
                new EmployeeItem { Id = 101, Name = "Vijay" },
            null!
            ]
        };

        var result = mapper.Map<Employee, EmployeeDto>(source);

        Assert.Equal(2, result.Employees!.Count);
        Assert.Equal("Vijay", result.Employees[0].Name);
        Assert.Null(result.Employees[1]);
    }

    [Fact]
    public void Map_ShouldMapListToArray()
    {
        var mapper = CreateMapper();

        var source = new EmployeeListSource
        {
            Employees =
            [
                new EmployeeItem { Id = 101, Name = "Vijay" },
            new EmployeeItem { Id = 102, Name = "Rahul" }
            ]
        };

        var result = mapper.Map<EmployeeListSource, EmployeeListToArrayDto>(source);

        Assert.Equal(2, result.Employees.Length);
        Assert.Equal("Vijay", result.Employees[0].Name);
        Assert.Equal("Rahul", result.Employees[1].Name);
    }


    [Fact]
    public void Map_ShouldMapListToIEnumerable()
    {
        var mapper = CreateMapper();

        var source = new InterfaceCollectionSource
        {
            Employees =
            [
                new EmployeeItem { Id = 101, Name = "Vijay" }
            ]
        };

        var result = mapper.Map<InterfaceCollectionSource, InterfaceCollectionDestination>(source);

        Assert.NotNull(result.Employees);
        Assert.Single(result.Employees);
        Assert.Equal("Vijay", result.Employees.First().Name);
    }

    [Fact]
    public void Map_ShouldMapListToIReadOnlyList()
    {
        var mapper = CreateMapper();

        var source = new InterfaceCollectionSource
        {
            Employees =
            [
                new EmployeeItem { Id = 101, Name = "Vijay" },
            new EmployeeItem { Id = 102, Name = "Rahul" }
            ]
        };

        var result = mapper.Map<InterfaceCollectionSource, ReadOnlyCollectionDestination>(source);

        Assert.NotNull(result.Employees);
        Assert.Equal(2, result.Employees.Count);
        Assert.Equal("Vijay", result.Employees[0].Name);
    }

    [Fact]
    public void Map_ShouldMapNestedObjectsInsideCollection()
    {
        var mapper = CreateMapper();

        var source = new CompanySource
        {
            Teams =
            [
                new TeamSource
            {
                Name = "Development",
                Department = new DepartmentSource
                {
                    Name = "Engineering",
                    Lead = new EmployeeSource
                    {
                        Name = "Vijay"
                    }
                }
            }
            ]
        };

        var result = mapper.Map<CompanySource, CompanyDestination>(source);

        Assert.Single(result.Teams);

        var team = result.Teams[0];

        Assert.Equal("Development", team.Name);
        Assert.NotNull(team.Department);
        Assert.Equal("Engineering", team.Department.Name);

        Assert.NotNull(team.Department.Lead);
        Assert.Equal("Vijay", team.Department.Lead.Name);
    }

    private static Mapper CreateMapper()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new EmployeeProfile());
        });

        return new Mapper(configuration);
    }

    public class CompanySource
    {
        public List<TeamSource> Teams { get; set; } = [];
    }

    public class TeamSource
    {
        public string Name { get; set; } = "";
        public DepartmentSource? Department { get; set; }
    }

    public class DepartmentSource
    {
        public string Name { get; set; } = "";
        public EmployeeSource? Lead { get; set; }
    }

    public class EmployeeSource
    {
        public string Name { get; set; } = "";
    }

    public class CompanyDestination
    {
        public List<TeamDestination> Teams { get; set; } = [];
    }

    public class TeamDestination
    {
        public string Name { get; set; } = "";
        public DepartmentDestination? Department { get; set; }
    }

    public class DepartmentDestination
    {
        public string Name { get; set; } = "";
        public EmployeeDestination? Lead { get; set; }
    }

    public class EmployeeDestination
    {
        public string Name { get; set; } = "";
    }

    public class EmployeeListSource
    {
        public List<EmployeeItem> Employees { get; set; } = [];
    }

    public class EmployeeListToArrayDto
    {
        public EmployeeItemDto[] Employees { get; set; } = [];
    }

    public class Employee
    {
        public int Id { get; set; }
        public List<EmployeeItem>? Employees { get; set; }
    }

    public class EmployeeItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class EmployeeDto
    {
        public int Id { get; set; }
        public List<EmployeeItemDto>? Employees { get; set; }
    }

    public class EmployeeItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class EmployeeArray
    {
        public EmployeeItem[] Employees { get; set; } = [];
    }

    public class EmployeeArrayDto
    {
        public EmployeeItemDto[] Employees { get; set; } = [];
    }

    public class NumberCollection
    {
        public List<string> Values { get; set; } = [];
    }

    public class NumberCollectionDto
    {
        public List<int> Values { get; set; } = [];
    }

    public class EmployeeProfile : MappingProfile
    {
        public EmployeeProfile()
        {
            CreateMap<EmployeeItem, EmployeeItemDto>();
            CreateMap<Employee, EmployeeDto>();

            CreateMap<EmployeeArray, EmployeeArrayDto>();
            CreateMap<NumberCollection, NumberCollectionDto>();

            CreateMap<InterfaceCollectionSource, InterfaceCollectionDestination>();
            CreateMap<InterfaceCollectionSource, ReadOnlyCollectionDestination>();

            CreateMap<EmployeeListSource, EmployeeListToArrayDto>();

            CreateMap<EmployeeSource, EmployeeDestination>();
            CreateMap<DepartmentSource, DepartmentDestination>();
            CreateMap<TeamSource, TeamDestination>();
            CreateMap<CompanySource, CompanyDestination>();
        }
    }


    public class InterfaceCollectionSource
    {
        public List<EmployeeItem> Employees { get; set; } = [];
    }

    public class InterfaceCollectionDestination
    {
        public IEnumerable<EmployeeItemDto>? Employees { get; set; }
    }

    public class ReadOnlyCollectionDestination
    {
        public IReadOnlyList<EmployeeItemDto>? Employees { get; set; }
    }

}