using AutoMapper;
using CloudShield.DTOs.State;
using Commons;
using DataContext;
using Entities.Users;
using Microsoft.EntityFrameworkCore;
using Services.StateServices;

namespace Repositories.States_Repository;

public class StatesRead_Repository : IReadCommandStates
{
  private readonly ApplicationDbContext _context;
  private readonly ILogger<StatesRead_Repository> _log;
  private readonly IMapper _mapper;
  public StatesRead_Repository(ApplicationDbContext context, ILogger<StatesRead_Repository> log, IMapper mapper)
  {
    _context = context;
    _log = log;
    _mapper = mapper;
  }
  public async Task<ApiResponse<List<StateDTO>>> GetAll()
  {
    try
    {
      List<State> listStates = await _context.State.ToListAsync();

      if (listStates.Count > 0)
      {
        List<StateDTO> found = _mapper.Map<List<StateDTO>>(listStates);
        return new ApiResponse<List<StateDTO>>(true, $"States Founded {listStates.Count}", found);
      }
      else
      {
        return new ApiResponse<List<StateDTO>>(false, $"any state found", null);
      }
    }
    catch (Exception ex)
    {
      _log.LogError(ex, ex.Message, "An Error Ocurred on Getting States");
      return new ApiResponse<List<StateDTO>>(false, ex.Message, null);
    }
  }

  public async Task<ApiResponse<StateDTO>> GetById(int stateId)
  {
    try
    {
      var state = await _context.State.FirstOrDefaultAsync(x => x.Id == stateId);

      if (state == null)
      {
        return new ApiResponse<StateDTO>(false, "State not found", null);
      }

      var stateDTO = _mapper.Map<StateDTO>(state);
      return new ApiResponse<StateDTO>(true, "State retrieved successfully", stateDTO);
    }
    catch (Exception ex)
    {
      _log.LogError(ex, "Error retrieving state by ID");
      return new ApiResponse<StateDTO>(false, "An error occurred while retrieving state", null);
    }
  }
}