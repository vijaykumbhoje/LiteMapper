using LiteMapper.Configuration;
using LiteMapper.Conversion;
using LiteMapper.Engine;
using Xunit;

namespace LiteMapper.Tests;

public class ThreadSafetyTests
{
    [Fact]
    public async Task Mapper_ShouldSupportConcurrentMapping()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new EmployeeMappingProfile());
        });

        var mapper = new Mapper(configuration);

        var tasks = Enumerable.Range(1, 1000)
            .Select(i => Task.Run(() =>
            {
                var source = new Employee
                {
                    Id = i,
                    Name = $"Employee-{i}"
                };

                var result =
                    mapper.Map<Employee, EmployeeDto>(source);

                Assert.Equal(i, result.Id);
                Assert.Equal($"Employee-{i}", result.Name);
            }));

        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task Mapper_ShouldIsolateCircularReferencesAcrossConcurrentOperations()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new CircularMappingProfile());
        });

        var mapper = new Mapper(configuration);

        var tasks = Enumerable.Range(1, 100)
            .Select(i => Task.Run(() =>
            {
                var employee = new CircularEmployee
                {
                    Id = i,
                    Name = $"Employee-{i}"
                };

                employee.Manager = employee;

                var result = mapper.Map<
                    CircularEmployee,
                    CircularEmployeeDto>(employee);

                Assert.Equal(i, result.Id);
                Assert.Equal($"Employee-{i}", result.Name);

                // Circular reference must point to the same DTO.
                Assert.Same(result, result.Manager);
            }));

        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task Mapper_ShouldSupportConcurrentCustomConverterExecution()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new CustomConverterMappingProfile());
            cfg.AddTypeConverter(new IntegerToStringConverter());
        });

        var mapper = new Mapper(configuration);

        var tasks = Enumerable.Range(1, 1000)
            .Select(i => Task.Run(() =>
            {
                var source = new NumericSource
                {
                    Amount = i
                };

                var result =
                    mapper.Map<NumericSource, NumericDestination>(source);

                Assert.Equal($"Value-{i}", result.Amount);
            }));

        await Task.WhenAll(tasks);
    }


    [Fact]
    public async Task Mapper_ShouldSupportConcurrentConfigurationLookups()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new EmployeeMappingProfile());
            cfg.AddProfile(new DepartmentMappingProfile());
        });

        var mapper = new Mapper(configuration);

        var tasks = Enumerable.Range(1, 1000)
            .Select(i => Task.Run(() =>
            {
                if (i % 2 == 0)
                {
                    var result = mapper.Map<Employee, EmployeeDto>(
                        new Employee
                        {
                            Id = i,
                            Name = $"Employee-{i}"
                        });

                    Assert.Equal(i, result.Id);
                }
                else
                {
                    var result = mapper.Map<Department, DepartmentDto>(
                        new Department
                        {
                            Id = i,
                            Name = $"Department-{i}"
                        });

                    Assert.Equal(i, result.Id);
                }
            }));

        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task Mapper_ShouldSupportMultipleCustomConvertersConcurrently()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new MultiConverterMappingProfile());

            cfg.AddTypeConverter<int, string, ConcurrentIntegerToStringConverter>();
            cfg.AddTypeConverter<decimal, string, DecimalToStringConverter>();
        });

        var mapper = new Mapper(configuration);

        var tasks = Enumerable.Range(1, 1000)
            .Select(i => Task.Run(() =>
            {
                if (i % 2 == 0)
                {
                    var result = mapper.Map<IntegerSource, IntegerDestination>(
                        new IntegerSource { Amount = i });

                    Assert.Equal($"INT-{i}", result.Amount);
                }
                else
                {
                    var result = mapper.Map<DecimalSource, DecimalDestination>(
                        new DecimalSource { Amount = i + 0.50m });

                    Assert.Equal($"DEC-{i + 0.50m:F2}", result.Amount);
                }
            }));

        await Task.WhenAll(tasks);
    }

    public class Employee
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class EmployeeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class CircularEmployee
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public CircularEmployee? Manager { get; set; }
    }

    public class CircularEmployeeDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public CircularEmployeeDto? Manager { get; set; }
    }

    public class NumericSource
    {
        public int Amount { get; set; }
    }

    public class NumericDestination
    {
        public string Amount { get; set; } = string.Empty;
    }

    public class Department
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class DepartmentDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class IntegerSource
    {
        public int Amount { get; set; }
    }

    public class IntegerDestination
    {
        public string Amount { get; set; } = string.Empty;
    }

    public class DecimalSource
    {
        public decimal Amount { get; set; }
    }

    public class DecimalDestination
    {
        public string Amount { get; set; } = string.Empty;
    }

    public class IntegerToStringConverter : ITypeConverter<int, string>
    {
        public string Convert(int source)
        {
            return $"Value-{source}";
        }
    }

    public class DecimalToStringConverter : ITypeConverter<decimal, string>
    {
        public string Convert(decimal source) => $"DEC-{source:F2}";
    }

    public class MultiConverterMappingProfile : MappingProfile
    {
        public MultiConverterMappingProfile()
        {
            CreateMap<IntegerSource, IntegerDestination>();
            CreateMap<DecimalSource, DecimalDestination>();
        }
    }
    public class DepartmentMappingProfile : MappingProfile
    {
        public DepartmentMappingProfile()
        {
            CreateMap<Department, DepartmentDto>();
        }
    }

    public class ConcurrentIntegerToStringConverter
     : ITypeConverter<int, string>
    {
        public string Convert(int source)
        {
            return $"INT-{source}";
        }
    }

    public class CustomConverterMappingProfile : MappingProfile
    {
        public CustomConverterMappingProfile()
        {
            CreateMap<NumericSource, NumericDestination>();
        }
    }

    public class CircularMappingProfile : MappingProfile
    {
        public CircularMappingProfile()
        {
            CreateMap<CircularEmployee, CircularEmployeeDto>();
        }
    }
    public class EmployeeMappingProfile : MappingProfile
    {
        public EmployeeMappingProfile()
        {
            CreateMap<Employee, EmployeeDto>();
        }
    }
}