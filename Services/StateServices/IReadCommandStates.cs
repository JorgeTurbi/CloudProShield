using CloudShield.DTOs.State;
using Commons;

namespace Services.StateServices;

public interface IReadCommandStates
{
  Task<ApiResponse<List<StateDTO>>> GetAll();
  Task<ApiResponse<StateDTO>> GetById(int stateId);
}