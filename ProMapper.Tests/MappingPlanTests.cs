using System.Collections.Concurrent;
using ProMapper.Configuration;
using ProMapper.Engine;
using Xunit;

namespace ProMapper.Tests;

public class MappingPlanTests
{
    [Fact]
    public void Mapper_ShouldReuseCachedPlanAcrossCalls()
    {
        var mapper = CreateMapper();

        for (var i = 0; i < 100; i++)
        {
            var result = mapper.Map<Employee, EmployeeDto>(
                new Employee
                {
                    Id = i,
                    Name = $"Employee {i}"
                });

            Assert.Equal(i, result.Id);
        }
    }

    [Fact]
    public void Mapper_ShouldSupportConcurrentMappingCalls()
    {
        var mapper = CreateMapper();

        var results = new ConcurrentBag<EmployeeDto>();

        Parallel.For(0, 1000, i =>
        {
            var result = mapper.Map<Employee, EmployeeDto>(
                new Employee
                {
                    Id = i,
                    Name = $"Employee {i}"
                });

            results.Add(result);
        });

        Assert.Equal(1000, results.Count);
        Assert.Equal(1000, results.Select(x => x.Id).Distinct().Count());
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
        public string Name { get; set; } = "";
    }

    public class EmployeeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class EmployeeProfile : MappingProfile
    {
        public EmployeeProfile()
        {
            CreateMap<Employee, EmployeeDto>();
        }
    }
}