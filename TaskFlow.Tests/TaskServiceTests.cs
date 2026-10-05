using System;
using System.Threading.Tasks;
using Moq;
using TaskFlow.Api.DTOs;
using TaskFlow.Api.Models;
using TaskFlow.Api.Repositories;
using TaskFlow.Api.Services;
using Xunit;
namespace TaskFlow.Tests;
public class TaskServiceTests
{
    [Fact]
    public async Task CreateAsync_CreatesTaskForCurrentUser()
    {
        var repo = new Mock<ITaskRepository>();
        var service = new TaskService(repo.Object);
        var result = await service.CreateAsync(new CreateTaskRequest("Prepare interview", "Review .NET", "HIGH", null, 7), 7, false);
        Assert.Equal("Prepare interview", result.Title); Assert.Equal("HIGH", result.Priority); repo.Verify(x => x.AddAsync(It.IsAny<TaskItem>()), Times.Once); repo.Verify(x => x.SaveChangesAsync(), Times.Once);
    }
    [Fact]
    public async Task CreateAsync_RejectsUserAssigningToSomeoneElse()
    {
        var service = new TaskService(new Mock<ITaskRepository>().Object);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateAsync(new CreateTaskRequest("X", null, null, null, 99), 7, false));
    }
}
