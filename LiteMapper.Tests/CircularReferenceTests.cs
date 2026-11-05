using LiteMapper.Configuration;
using LiteMapper.Engine;
using Xunit;

namespace LiteMapper.Tests;

public class CircularReferenceTests
{
    [Fact]
    public void Map_ShouldHandleCircularReference()
    {
        var mapper = CreateMapper();

        var employee = new Employee
        {
            Id = 1
        };

        // Employee refers to itself.
        employee.Manager = employee;

        var result = mapper.Map<Employee, EmployeeDto>(employee);

        Assert.Equal(1, result.Id);

        // The manager should point to the already-created destination.
        Assert.Same(result, result.Manager);
    }

    [Fact]
    public void Map_ShouldThrowWhenMaximumDepthExceeded()
    {
        var mapper = CreateMapper();

        var root = new Node { Id = 1 };
        var current = root;

        // Create a chain deeper than the default limit of 64.
        for (int i = 2; i <= 70; i++)
        {
            current.Child = new Node { Id = i };
            current = current.Child;
        }

        var exception = Assert.Throws<InvalidOperationException>(
            () => mapper.Map<Node, NodeDto>(root));

        Assert.Contains("Maximum mapping depth", exception.Message);
    }

    [Fact]
    public void Map_ShouldHandleParentChildCircularReference()
    {
        var mapper = CreateMapper();

        var parent = new Employee { Id = 1 };
        var child = new Employee { Id = 2 };

        parent.Manager = child;
        child.Manager = parent;

        var result = mapper.Map<Employee, EmployeeDto>(parent);

        Assert.Equal(1, result.Id);
        Assert.NotNull(result.Manager);
        Assert.Equal(2, result.Manager.Id);

        // The child's manager should refer to the original destination.
        Assert.Same(result, result.Manager.Manager);
    }

    [Fact]
    public void Map_ShouldPreserveSharedObjectReferences()
    {
        var mapper = CreateMapper();

        var sharedNode = new SharedNode { Id = 100 };

        var source = new SharedReferenceSource
        {
            First = sharedNode,
            Second = sharedNode
        };

        var result = mapper.Map<SharedReferenceSource, SharedReferenceDestination>(source);

        Assert.NotNull(result.First);
        Assert.NotNull(result.Second);

        Assert.Equal(100, result.First.Id);
        Assert.Same(result.First, result.Second);
    }

    private static Mapper CreateMapper()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new TestProfile());
        });

        return new Mapper(configuration);
    }

    public class SharedReferenceSource
    {
        public SharedNode? First { get; set; }
        public SharedNode? Second { get; set; }
    }

    public class SharedReferenceDestination
    {
        public SharedNodeDto? First { get; set; }
        public SharedNodeDto? Second { get; set; }
    }

    public class SharedNode
    {
        public int Id { get; set; }
    }

    public class SharedNodeDto
    {
        public int Id { get; set; }
    }
    public class Employee
    {
        public int Id { get; set; }
        public Employee? Manager { get; set; }
    }

    public class EmployeeDto
    {
        public int Id { get; set; }
        public EmployeeDto? Manager { get; set; }
    }

    public class Node
    {
        public int Id { get; set; }
        public Node? Child { get; set; }
    }

    public class NodeDto
    {
        public int Id { get; set; }
        public NodeDto? Child { get; set; }
    }

    public class TestProfile : MappingProfile
    {
        public TestProfile()
        {
            CreateMap<Employee, EmployeeDto>();
            CreateMap<Node, NodeDto>();

            CreateMap<SharedNode, SharedNodeDto>();
            CreateMap<SharedReferenceSource, SharedReferenceDestination>();
        }
    }
}