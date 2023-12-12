﻿using Excellerent.ResourceManagement.Domain.Interfaces.Services;
using Excellerent.SharedModules.DTO;
using Excellerent.SharedModules.Services;
using Excellerent.Timesheet.Domain.Dtos;
using Excellerent.Timesheet.Domain.Entities;
using Excellerent.Timesheet.Domain.Interfaces.Repository;
using Excellerent.Timesheet.Domain.Interfaces.Service;
using Excellerent.Timesheet.Domain.Mapping;
using Excellerent.Timesheet.Domain.Models;
using LinqKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Excellerent.Timesheet.Domain.Services
{
    public class TimeEntryService : CRUD<TimeEntryEntity, TimeEntry>, ITimeEntryService
    {
        private readonly ITimeEntryRepository _timeEntryRepository;
        private readonly ITimeSheetService _timesheetService;
        private readonly ITimesheetApprovalService _timesheetAprovalService;
        private readonly ITimeEntryValidationService _timeEntryValidationService;

        public TimeEntryService(ITimeEntryRepository repository,
            ITimeSheetService timeSheetService,
            ITimesheetApprovalService timesheetAprovalService,
            ITimeSheetConfigService timeSheetConfigService,
            IEmployeeService employeeService) : base(repository)
        {
            _timeEntryRepository = repository;
            _timesheetService = timeSheetService;
            _timesheetAprovalService = timesheetAprovalService;
            _timeEntryValidationService = new TimeEntryValidationService(this, timeSheetService, timesheetAprovalService, timeSheetConfigService, employeeService );
        }

        public async Task<ResponseDTO> ApproveTimeSheet(Guid timesheetGuid)
        {
            try
            {
                await _timeEntryValidationService.ValidateMinimumWorkingDayAndWorkingHourForApproval(timesheetGuid);

                List<TimesheetApprovalEntity> timesheetApprovalEntities = new List<TimesheetApprovalEntity>();

                var timeEntries = _timeEntryRepository.FindAsync(timeEntry => timeEntry.TimesheetGuid == timesheetGuid).Result.ToList();

                if (timeEntries == null || timeEntries.Count == 0)
                {
                    return null;
                }

                foreach (var timeEntry in timeEntries)
                {
                    var timesheetApprovalEntity = timesheetApprovalEntities.Find(tse => tse.TimesheetId == timeEntry.TimesheetGuid && tse.ProjectId == timeEntry.ProjectId);

                    if (timesheetApprovalEntity != null) { continue; }

                    timesheetApprovalEntity = new TimesheetApprovalEntity
                    {
                        TimesheetId = timeEntry.TimesheetGuid,
                        ProjectId = timeEntry.ProjectId,
                        Status = (int)ApprovalStatus.Requested
                    };

                    await _timesheetAprovalService.Add(timesheetApprovalEntity);

                    timesheetApprovalEntities.Add(timesheetApprovalEntity);
                }

                return new ResponseDTO(ResponseStatus.Success,"",timesheetApprovalEntities.Select(tsa => tsa.MapToDto()).ToList());
            }
            catch (Exception ex)
            {
                return new ResponseDTO(ResponseStatus.Error, ex.Message, null);
            }
        }

        public async Task<ResponseDTO> GetTimeEntry(Guid timeEntryId)
        {
            try
            {
                var predicate = PredicateBuilder.New<TimeEntry>();

                predicate = predicate.And(timeEntry => timeEntry.Guid == timeEntryId);

                TimeEntry timeEntry = await _timeEntryRepository.GetTimeEntry(predicate);

                return new ResponseDTO(ResponseStatus.Success, "Time Entry by Id", timeEntry?.MapToDto());
            }
            catch (Exception ex)
            {
                return new ResponseDTO(ResponseStatus.Error, ex.Message, null);
            }
        }

        public async Task<ResponseDTO> GetTimeEntryForUpdateOrDelete(Guid timeEntryId)
        {
            try
            {
                var predicate = PredicateBuilder.New<TimeEntry>();

                predicate = predicate.And(timeEntry => timeEntry.Guid == timeEntryId);

                TimeEntry timeEntry = await _timeEntryRepository.GetTimeEntryForUpdateOrDelete(predicate);

                return new ResponseDTO(ResponseStatus.Success, "Time Entry by Id for Update or Delete", timeEntry?.MapToDto());
            }
            catch (Exception ex)
            {
                return new ResponseDTO(ResponseStatus.Error, ex.Message, null);
            }
        }

        public async Task<ResponseDTO> GetTimeEntries(Guid timeSheetId, DateTime? date, Guid? projectId)
        {
            try
            {
                var predicate = PredicateBuilder.New<TimeEntry>();

                predicate = predicate.And(timeEntry => timeEntry.TimesheetGuid == timeSheetId);

                if (date != null)
                {
                    predicate = predicate.And(timeEntry => timeEntry.Date == date);
                }

                if (projectId != null)
                {
                    predicate = predicate.And(timeEntry => timeEntry.ProjectId == projectId);
                }

                var timeEntries = await _timeEntryRepository.GetTimeEntries(predicate);

                return new ResponseDTO(ResponseStatus.Success, "List of Time Entry by timesheet, date, and project Id", timeEntries.Select(x => x.MapToDto()).ToList());
            }
            catch (Exception ex)
            {
                return new ResponseDTO(ResponseStatus.Error, ex.Message, null);
            }
        }

        public async Task<ResponseDTO> AddTimeEntry(Guid employeeId, TimeEntryDto timeEntryDto)
        {
            try
            {
                await _timeEntryValidationService.ValidateTimeEntry(employeeId, timeEntryDto);

                ResponseDTO timesheetResponse = await _timesheetService.GetTimeSheet(employeeId, timeEntryDto.Date);

                if (timesheetResponse.ResponseStatus == ResponseStatus.Error)
                {
                    return timesheetResponse;
                }
                else if (timesheetResponse.Data != null)
                {
                    timeEntryDto.TimeSheetId = (timesheetResponse.Data as TimeSheetDto).Guid;
                    timeEntryDto.Guid = Guid.NewGuid();
                    timeEntryDto.Date = (DateTime)timeEntryDto.Date.Date;

                    var timeEntry = await _timeEntryRepository.AddTimeEntry(timeEntryDto.MapToModel());

                    return new ResponseDTO(ResponseStatus.Success, "Time Entry Added Successfully", timeEntryDto);
                }
                else
                {
                    timeEntryDto.Guid = Guid.NewGuid();
                    timeEntryDto.Date = (DateTime)timeEntryDto.Date.Date;
                    return await _timesheetService.AddTimeSheet(employeeId, timeEntryDto);
                }
            }
            catch (Exception ex)
            {
                return new ResponseDTO(ResponseStatus.Error, ex.Message, null);
            }
        }

        public async Task<ResponseDTO> UpdateTimeEntry(TimeEntryDto timeEntryDto)
        {
            try
            {
                await _timeEntryValidationService.ValidateTimeEntry(timeEntryDto);

                ResponseDTO timeEntryResponse = await GetTimeEntryForUpdateOrDelete(timeEntryDto.Guid);

                if (timeEntryResponse.ResponseStatus == ResponseStatus.Error)
                {
                    return timeEntryResponse;
                }
                else if (timeEntryResponse.Data != null)
                {
                    await _timeEntryRepository.UpdateTimeEntry(timeEntryDto.MapToModel());

                    return new ResponseDTO(ResponseStatus.Success, "TimeEntry updated successfully.", null);
                }
                else
                {
                    return new ResponseDTO(ResponseStatus.Info, "Can not update nonexistent TimeEntry", null);
                }
            }
            catch (Exception ex)
            {
                return new ResponseDTO(ResponseStatus.Error, ex.Message, null);
            }
        }

        public async Task<ResponseDTO> AddTImeEntryForRangeOfDays(Guid employeeId, TimeEntryDto[] entries)
        {
            try
            {
                string message;
                ResponseStatus status;
                List<ResponseDTO> responses = new List<ResponseDTO>();
                for (int i = 0; i < entries.Length; i++)
                {
                    var prev = await _timeEntryRepository.GetTimeEntryForUpdateOrDelete(x => x.Guid == entries[i].MapToModel().Guid);
                    var result = prev != null ? await this.UpdateTimeEntry(entries[i]) : await this.AddTimeEntry(employeeId, entries[i]);
                    responses.Add(result);
                }
                if ((responses.Find(x => x.ResponseStatus == ResponseStatus.Error || x.ResponseStatus == ResponseStatus.Warning) != null))
                {
                    message = "Some entries were not added successfully.";
                    status = ResponseStatus.Warning;
                }
                else
                {
                    message = "Time Entries added successfully";
                    status = ResponseStatus.Success;
                }
                return new ResponseDTO(status, message, responses);
            }
            catch (Exception ex)
            {
                return new ResponseDTO(ResponseStatus.Error, ex.Message, null);
            }
        }

        public async Task<ResponseDTO> RemoveTimeEntryById(Guid timeEntryId)
        {
            try
            {
                var entryToDelete = await _timeEntryRepository.GetByGuidAsync(timeEntryId);
                if (entryToDelete != null)
                {
                    await _timeEntryValidationService.ValidateDeleteTimeEntry(entryToDelete.MapToDto());
                    await _timeEntryRepository.DeleteAsync(entryToDelete);
                    return new ResponseDTO(ResponseStatus.Success, "Time Entry deleted successfully", entryToDelete);
                }
                else
                {
                    return new ResponseDTO(ResponseStatus.Error, "TimeEntry doesnot exist", null);
                }
            }
            catch (Exception ex)
            {
                return new ResponseDTO(ResponseStatus.Error, ex.Message, null);
            }
        }

        public async Task<ResponseDTO> RemoveTimeEntry(TimeEntryDto timeEntryDto)
        {
            try
            {
                await _timeEntryValidationService.ValidateDeleteTimeEntry(timeEntryDto);
                await _timeEntryRepository.DeleteAsync(timeEntryDto.MapToModel());
                return new ResponseDTO(ResponseStatus.Success, "Time Entry deleted successfully", timeEntryDto);
            }
            catch (Exception ex)
            {
                return new ResponseDTO(ResponseStatus.Error, ex.Message, null);
            }
        }
    }

}

