using ProMapper.Configuration;
using ProMapper.Engine;
using Xunit;

namespace ProMapper.Tests;

public class NestedMappingTests
{
    [Fact]
    public void Map_ShouldMapNestedObject()
    {
        // Arrange
        var mapper = CreateMapper();

        var employee = new Employee
        {
            Id = 101,
            Name = "Vijay",
            Department = new Department
            {
                Id = 10,
                Name = "Engineering"
            }
        };

        // Act
        var result = mapper.Map<Employee, EmployeeDto>(employee);

        // Assert
        Assert.Equal(101, result.Id);
        Assert.Equal("Vijay", result.Name);

        Assert.NotNull(result.Department);
        Assert.Equal(10, result.Department.Id);
        Assert.Equal("Engineering", result.Department.Name);
    }

    [Fact]
    public void Map_ShouldHandleNullNestedObject()
    {
        var mapper = CreateMapper();

        var employee = new Employee
        {
            Id = 102,
            Name = "Test User",
            Department = null
        };

        var result = mapper.Map<Employee, EmployeeDto>(employee);

        Assert.Null(result.Department);
    }


    [Fact]
    public void Map_ShouldMapNestedObjectWithDefaultValues()
    {
        var mapper = CreateMapper();

        var employee = new Employee
        {
            Id = 103,
            Name = "Default Department",
            Department = new Department()
        };

        var result = mapper.Map<Employee, EmployeeDto>(employee);

        Assert.NotNull(result.Department);
        Assert.Equal(0, result.Department.Id);
        Assert.Equal(string.Empty, result.Department.Name);
    }

    [Fact]
    public void Map_ShouldMapDeeplyNestedObjects()
    {
        var mapper = CreateMapper();

        var source = new EmployeeWithManager
        {
            Id = 104,
            Manager = new Manager
            {
                Name = "Rahul",
                Department = new Department
                {
                    Id = 20,
                    Name = "Technology"
                }
            }
        };

        var result = mapper.Map<EmployeeWithManager, EmployeeWithManagerDto>(source);

        Assert.NotNull(result.Manager);
        Assert.Equal("Rahul", result.Manager.Name);
        Assert.NotNull(result.Manager.Department);
        Assert.Equal(20, result.Manager.Department.Id);
        Assert.Equal("Technology", result.Manager.Department.Name);
    }

    [Fact]
    public void Map_ShouldUpdateExistingNestedDestinationObject()
    {
        var mapper = CreateMapper();

        var source = new Employee
        {
            Id = 105,
            Name = "Vijay",
            Department = new Department
            {
                Id = 30,
                Name = "Architecture"
            }
        };

        var destination = new EmployeeDto
        {
            Department = new DepartmentDto
            {
                Id = 99,
                Name = "Old Department"
            }
        };

        var result = mapper.Map<Employee, EmployeeDto>(source, destination);

        Assert.NotNull(result.Department);
        Assert.Equal(30, result.Department.Id);
        Assert.Equal("Architecture", result.Department.Name);
    }


    private static Mapper CreateMapper()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new EmployeeProfile());
        });

        return new Mapper(configuration);
    }


    public class EmployeeWithManager
    {
        public int Id { get; set; }
        public Manager? Manager { get; set; }
    }

    public class Manager
    {
        public string Name { get; set; } = "";
        public Department? Department { get; set; }
    }

    public class EmployeeWithManagerDto
    {
        public int Id { get; set; }
        public ManagerDto? Manager { get; set; }
    }

    public class ManagerDto
    {
        public string Name { get; set; } = "";
        public DepartmentDto? Department { get; set; }
    }


    public class Employee
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public Department? Department { get; set; }
    }

    public class Department
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class EmployeeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public DepartmentDto? Department { get; set; }
    }

    public class DepartmentDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class EmployeeProfile : MappingProfile
    {
        public EmployeeProfile()
        {
            CreateMap<Department, DepartmentDto>();

            CreateMap<Employee, EmployeeDto>();

            CreateMap<Manager, ManagerDto>();
            CreateMap<EmployeeWithManager, EmployeeWithManagerDto>();
        }
    }
}