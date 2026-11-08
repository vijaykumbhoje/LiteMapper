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


    [Fact]
    public void Map_ShouldMapListToICollection()
    {
        var mapper = CreateMapper();

        var source = new Employee
        {
            Employees =
            [
                new EmployeeItem { Id = 101, Name = "Vijay" },
            new EmployeeItem { Id = 102, Name = "Rahul" }
            ]
        };

        var result = mapper.Map<Employee, ICollectionDestination>(source);

        Assert.NotNull(result.Employees);
        Assert.Equal(2, result.Employees.Count);
        Assert.Equal("Vijay", result.Employees.First().Name);
    }

    [Fact]
    public void Map_ShouldMapListToIReadOnlyCollection()
    {
        var mapper = CreateMapper();

        var source = new Employee
        {
            Employees =
            [
                new EmployeeItem { Id = 101, Name = "Vijay" },
            new EmployeeItem { Id = 102, Name = "Rahul" }
            ]
        };

        var result = mapper.Map<Employee, ReadOnlyCollectionInterfaceDestination>(source);

        Assert.NotNull(result.Employees);
        Assert.Equal(2, result.Employees.Count);
    }

    [Fact]
    public void Map_ShouldMapListToHashSet()
    {
        var mapper = CreateMapper();

        var source = new Employee
        {
            Employees =
            [
                new EmployeeItem { Id = 101, Name = "Vijay" },
            new EmployeeItem { Id = 102, Name = "Rahul" }
            ]
        };

        var result = mapper.Map<Employee, HashSetDestination>(source);

        Assert.NotNull(result.Employees);
        Assert.Equal(2, result.Employees.Count);
        Assert.Contains(result.Employees, x => x.Name == "Vijay");
        Assert.Contains(result.Employees, x => x.Name == "Rahul");
    }

    [Fact]
    public void Map_ShouldRejectUnsupportedCollectionType()
    {
        var mapper = CreateMapper();

        var source = new Employee
        {
            Employees =
            [
                new EmployeeItem { Id = 101, Name = "Vijay" }
            ]
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => mapper.Map<Employee, LinkedListDestination>(source));

        Assert.Contains("Cannot convert value", exception.Message);
    }

    [Fact]
    public void Map_ShouldMapListToISet()
    {
        var mapper = CreateMapper();

        var source = new Employee
        {
            Employees =
            [
                new EmployeeItem { Id = 101, Name = "Vijay" },
            new EmployeeItem { Id = 102, Name = "Rahul" }
            ]
        };

        var result = mapper.Map<Employee, SetInterfaceDestination>(source);

        Assert.NotNull(result.Employees);
        Assert.Equal(2, result.Employees.Count);
        Assert.Contains(result.Employees, x => x.Name == "Vijay");
    }


    [Fact]
    public void Map_ShouldMapListToQueue()
    {
        var mapper = CreateMapper();

        var source = new Employee
        {
            Employees =
            [
                new EmployeeItem { Id = 101, Name = "Vijay" },
            new EmployeeItem { Id = 102, Name = "Rahul" },
            new EmployeeItem { Id = 103, Name = "Amit" }
            ]
        };

        var result = mapper.Map<Employee, QueueDestination>(source);

        Assert.NotNull(result.Employees);
        Assert.Equal(3, result.Employees.Count);

        Assert.Equal(
            new[] { "Vijay", "Rahul", "Amit" },
            result.Employees.Select(x => x.Name));
    }

    [Fact]
    public void Map_ShouldMapListToStack()
    {
        var mapper = CreateMapper();

        var source = new Employee
        {
            Employees =
            [
                new EmployeeItem { Id = 101, Name = "Vijay" },
            new EmployeeItem { Id = 102, Name = "Rahul" },
            new EmployeeItem { Id = 103, Name = "Amit" }
            ]
        };

        var result = mapper.Map<Employee, StackDestination>(source);

        Assert.NotNull(result.Employees);
        Assert.Equal(3, result.Employees.Count);

        // Stack enumeration should preserve source enumeration order.
        Assert.Equal(
            new[] { "Amit", "Rahul", "Vijay" },
            result.Employees.Select(x => x.Name));
    }


    [Fact]
    public void Map_ShouldReuseExistingHashSetDestination()
    {
        var mapper = CreateMapper();

        var source = new Employee
        {
            Employees =
            [
                new EmployeeItem { Id = 101, Name = "Vijay" },
            new EmployeeItem { Id = 102, Name = "Rahul" }
            ]
        };

        var existingSet = new HashSet<EmployeeItemDto>
    {
        new() { Id = 999, Name = "Existing" }
    };

        var destination = new HashSetDestination
        {
            Employees = existingSet
        };

        var result = mapper.Map(source, destination);

        Assert.Same(existingSet, result.Employees);
        Assert.Equal(2, result.Employees!.Count);
        Assert.Contains(result.Employees, x => x.Name == "Vijay");
        Assert.Contains(result.Employees, x => x.Name == "Rahul");
        Assert.DoesNotContain(result.Employees, x => x.Name == "Existing");
    }

    [Fact]
    public void Map_ShouldReuseExistingISetDestination()
    {
        var mapper = CreateMapper();

        var source = new Employee
        {
            Employees =
            [
                new EmployeeItem { Id = 101, Name = "Vijay" }
            ]
        };

        ISet<EmployeeItemDto> existingSet =
            new HashSet<EmployeeItemDto>
            {
            new() { Id = 999, Name = "Existing" }
            };

        var destination = new SetInterfaceDestination
        {
            Employees = existingSet
        };

        var result = mapper.Map(source, destination);

        Assert.Same(existingSet, result.Employees);
        Assert.Single(result.Employees!);
        Assert.Equal("Vijay", result.Employees.First().Name);
        Assert.DoesNotContain(
            result.Employees,
            x => x.Name == "Existing");
    }

    [Fact]
    public void Map_ShouldReuseExistingQueueDestination()
    {
        var mapper = CreateMapper();

        var source = new Employee
        {
            Employees =
            [
                new EmployeeItem { Id = 101, Name = "Vijay" },
            new EmployeeItem { Id = 102, Name = "Rahul" }
            ]
        };

        var existingQueue = new Queue<EmployeeItemDto>();

        existingQueue.Enqueue(
            new EmployeeItemDto { Id = 999, Name = "Existing" });

        var destination = new QueueDestination
        {
            Employees = existingQueue
        };

        var result = mapper.Map(source, destination);

        Assert.Same(existingQueue, result.Employees);
        Assert.Equal(
            new[] { "Vijay", "Rahul" },
            result.Employees!.Select(x => x.Name));
    }

    [Fact]
    public void Map_ShouldReuseExistingStackDestination()
    {
        var mapper = CreateMapper();

        var source = new Employee
        {
            Employees =
            [
                new EmployeeItem { Id = 101, Name = "Vijay" },
            new EmployeeItem { Id = 102, Name = "Rahul" },
            new EmployeeItem { Id = 103, Name = "Amit" }
            ]
        };

        var existingStack = new Stack<EmployeeItemDto>();

        existingStack.Push(
            new EmployeeItemDto { Id = 999, Name = "Existing" });

        var destination = new StackDestination
        {
            Employees = existingStack
        };

        var result = mapper.Map(source, destination);

        Assert.Same(existingStack, result.Employees);
        Assert.Equal(
       new[] { "Amit", "Rahul", "Vijay" },
       result.Employees.Select(x => x.Name));
        Assert.DoesNotContain(
            result.Employees,
            x => x.Name == "Existing");
    }


    [Fact]
    public void Map_ShouldSetDestinationCollectionToNull_WhenSourceCollectionIsNull()
    {
        var mapper = CreateMapper();

        var source = new Employee
        {
            Employees = null
        };

        var destination = new EmployeeDto
        {
            Employees = new List<EmployeeItemDto>
        {
            new() { Id = 1, Name = "Existing" }
        }
        };

        var result = mapper.Map(source, destination);

        Assert.Null(result.Employees);
    }

    [Fact]
    public void Map_ShouldClearExistingCollection_WhenSourceCollectionIsEmpty()
    {
        var mapper = CreateMapper();

        var source = new Employee
        {
            Employees = new List<EmployeeItem>()
        };

        var existingItems = new List<EmployeeItemDto>
    {
        new() { Id = 1, Name = "Existing" }
    };

        var destination = new EmployeeDto
        {
            Employees = existingItems
        };

        var result = mapper.Map(source, destination);

        Assert.Empty(result.Employees!);
        Assert.Same(existingItems, result.Employees);
    }

    [Fact]
    public void Map_ShouldReplaceReadOnlyCollection_WhenDestinationPropertyHasSetter()
    {
        var mapper = CreateMapper();

        var source = new Employee
        {
            Employees = new List<EmployeeItem>
        {
            new() { Id = 2, Name = "New" }
        }
        };

        var existingItems = Array.AsReadOnly(
            new[]
            {
            new EmployeeItemDto { Id = 1, Name = "Existing" }
            });

        var destination = new ReadOnlyCollectionDestination
        {
            Employees = existingItems
        };

        var result = mapper.Map(source, destination);

        Assert.NotSame(existingItems, result.Employees);
        Assert.Single(result.Employees!);
        Assert.Equal(2, result.Employees![0].Id);
        Assert.Equal("New", result.Employees[0].Name);
    }


    [Fact]
    public void Map_ShouldReuseExistingCollection_WhenSourceAndDestinationCollectionTypesAreSame()
    {
        var mapper = CreateMapper();

        var sourceItems = new List<EmployeeItem>
    {
        new() { Id = 2, Name = "New Employee" }
    };

        var source = new Employee
        {
            Employees = sourceItems
        };

        var existingItems = new List<EmployeeItem>
    {
        new() { Id = 1, Name = "Existing Employee" }
    };

        var destination = new SameTypeCollectionDestination
        {
            Employees = existingItems
        };

        var result = mapper.Map(source, destination);

        Assert.Same(existingItems, result.Employees);
        Assert.NotSame(sourceItems, result.Employees);
        Assert.Single(result.Employees!);
        Assert.Equal(2, result.Employees![0].Id);
        Assert.Equal("New Employee", result.Employees[0].Name);
    }

    [Fact]
    public void Map_ShouldNotAssignSourceCollectionDirectly_WhenDestinationCollectionIsNull()
    {
        var mapper = CreateMapper();

        var sourceItems = new List<EmployeeItem>
    {
        new() { Id = 2, Name = "New Employee" }
    };

        var source = new Employee
        {
            Employees = sourceItems
        };

        var destination = new SameTypeCollectionDestination
        {
            Employees = null
        };

        var result = mapper.Map(source, destination);

        Assert.NotSame(sourceItems, result.Employees);
        Assert.Single(result.Employees!);
        Assert.Equal(2, result.Employees![0].Id);
    }


    [Fact]
    public void Map_ShouldPopulateGetterOnlyCollection()
    {
        var mapper = CreateMapper();

        var source = new Employee
        {
            Employees = new List<EmployeeItem>
        {
            new() { Id = 10, Name = "Employee A" },
            new() { Id = 20, Name = "Employee B" }
        }
        };

        var destination = new GetterOnlyCollectionDestination();

        var existingCollection = destination.Employees;

        var result = mapper.Map(source, destination);

        Assert.Same(existingCollection, result.Employees);
        Assert.Equal(2, result.Employees.Count);
        Assert.Equal(10, result.Employees[0].Id);
        Assert.Equal("Employee A", result.Employees[0].Name);
        Assert.Equal(20, result.Employees[1].Id);
    }

    [Fact]
    public void Map_ShouldPopulateGetterOnlyHashSet()
    {
        var mapper = CreateMapper();

        var source = new Employee
        {
            Employees = new List<EmployeeItem>
        {
            new() { Id = 10, Name = "A" },
            new() { Id = 20, Name = "B" }
        }
        };

        var destination = new GetterOnlyHashSetDestination();
        var existing = destination.Employees;

        var result = mapper.Map(source, destination);

        Assert.Same(existing, result.Employees);
        Assert.Equal(2, result.Employees.Count);
        Assert.Contains(result.Employees, x => x.Id == 10);
        Assert.Contains(result.Employees, x => x.Id == 20);
    }

    [Fact]
    public void Map_ShouldPopulateGetterOnlyQueue()
    {
        var mapper = CreateMapper();

        var source = new Employee
        {
            Employees = new List<EmployeeItem>
        {
            new() { Id = 10, Name = "A" },
            new() { Id = 20, Name = "B" }
        }
        };

        var destination = new GetterOnlyQueueDestination();
        var existing = destination.Employees;

        var result = mapper.Map(source, destination);

        Assert.Same(existing, result.Employees);
        Assert.Equal(new[] { 10, 20 },
            result.Employees.Select(x => x.Id));
    }

    [Fact]
    public void Map_ShouldPopulateGetterOnlyStack()
    {
        var mapper = CreateMapper();

        var source = new Employee
        {
            Employees = new List<EmployeeItem>
        {
            new() { Id = 10, Name = "A" },
            new() { Id = 20, Name = "B" }
        }
        };

        var destination = new GetterOnlyStackDestination();
        var existing = destination.Employees;

        var result = mapper.Map(source, destination);

        Assert.Same(existing, result.Employees);
        Assert.Equal(new[] { 20, 10 },
            result.Employees.Select(x => x.Id));
    }

    private static Mapper CreateMapper()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new EmployeeProfile());
        });

        return new Mapper(configuration);
    }

    public class ICollectionDestination
    {
        public ICollection<EmployeeItemDto>? Employees { get; set; }
    }

    public class ReadOnlyCollectionInterfaceDestination
    {
        public IReadOnlyCollection<EmployeeItemDto>? Employees { get; set; }
    }


    public class SameTypeCollectionDestination
    {
        public List<EmployeeItem>? Employees { get; set; }
    }

    public class HashSetDestination
    {
        public HashSet<EmployeeItemDto>? Employees { get; set; }
    }

    public class QueueDestination
    {
        public Queue<EmployeeItemDto>? Employees { get; set; }
    }

    public class LinkedListDestination
    {
        public LinkedList<EmployeeItemDto>? Employees { get; set; }
    }

    public class CompanySource
    {
        public List<TeamSource> Teams { get; set; } = [];
    }

    public class SetInterfaceDestination
    {
        public ISet<EmployeeItemDto>? Employees { get; set; }
    }

    public class GetterOnlyHashSetDestination
    {
        public HashSet<EmployeeItemDto> Employees { get; } = new();
    }

    public class GetterOnlyQueueDestination
    {
        public Queue<EmployeeItemDto> Employees { get; } = new();
    }

    public class GetterOnlyStackDestination
    {
        public Stack<EmployeeItemDto> Employees { get; } = new();
    }

    public class GetterOnlyCollectionDestination
    {
        public List<EmployeeItemDto> Employees { get; } = new();
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

    public class StackDestination
    {
        public Stack<EmployeeItemDto>? Employees { get; set; }
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

            CreateMap<Employee, ICollectionDestination>();
            CreateMap<Employee, ReadOnlyCollectionInterfaceDestination>();
            CreateMap<Employee, HashSetDestination>();
            CreateMap<Employee, QueueDestination>();

            CreateMap<Employee, SetInterfaceDestination>();
            CreateMap<Employee, StackDestination>();
            CreateMap<Employee, LinkedListDestination>();

            CreateMap<Employee, ReadOnlyCollectionDestination>();
            CreateMap<Employee, SameTypeCollectionDestination>();

            CreateMap<Employee, GetterOnlyCollectionDestination>();

            CreateMap<Employee, GetterOnlyHashSetDestination>();
            CreateMap<Employee, GetterOnlyQueueDestination>();
            CreateMap<Employee, GetterOnlyStackDestination>();
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