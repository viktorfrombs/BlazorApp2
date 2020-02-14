using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlazorApp2.Services.Interfaces
{
    public interface IGraphService2
    {
        Task<IEnumerable<string>> CheckMemberGroupsAsync(string userToken, IEnumerable<string> groupIds);
        Task<List<string>> GetAllUserDisplayNames(string userToken);
    }
}
