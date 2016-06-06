using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransferDesk.Contracts.Manuscript.DTO;
using Repositories = TransferDesk.DAL.Manuscript.Repositories;
using DataContext = TransferDesk.DAL.Manuscript.DataContext;
using TransferDesk.DAL.Manuscript.Repositories;
using TransferDesk.DAL.Manuscript.UnitOfWork;
using TransferDesk.Contracts.Manuscript.Entities;
using TransferDesk.Contracts.Manuscript.ComplexTypes.UserRole;
using System.IO;
namespace TransferDesk.BAL.Manuscript
{
    public class ManuscriptLoginBL
    {
        private DataContext.ManuscriptDBContext context = null;
        public ManuscriptDBRepositoryReadSide _manuscriptDBRepositoryReadSide { get; set; }
        public ManuscriptLoginDBRepositoryReadSide _manuscriptLoginDBRepositoryReadSide { get; set; }
        public ManuscriptLoginDetailsRepository _manuscriptLoginDetailsRepository { get; set; }
        public Impersonation.Impersonate impersonate = null;
        string conString;

        public ManuscriptLoginBL(string conString)
        {
            this.conString = conString;
            context = new DataContext.ManuscriptDBContext(conString);
            _manuscriptDBRepositoryReadSide = new ManuscriptDBRepositoryReadSide(conString);
            _manuscriptLoginDBRepositoryReadSide = new ManuscriptLoginDBRepositoryReadSide(conString);
            _manuscriptLoginDetailsRepository = new ManuscriptLoginDetailsRepository(conString);
        }

        public bool SaveManuscriptLogin(ManuscriptLoginDTO manuscriptLoginDTO, IDictionary<string, string> dataErrors)
        {
            var manuscriptStatus = _manuscriptDBRepositoryReadSide.GetManuscriptStatus();
            var manuscriptStatusId = (from q in manuscriptStatus
                                      where q.Description.ToLower() == "open"
                                      select q.ID).FirstOrDefault();
            manuscriptLoginDTO.manuscriptLogin.ManuscriptStatusId = Convert.ToInt32(manuscriptStatusId);
            if (manuscriptLoginDTO.IsRevision == true)
            {
                manuscriptLoginDTO.manuscriptLogin.Revision = Convert.ToInt32(_manuscriptLoginDBRepositoryReadSide.GetRevisionCount(manuscriptLoginDTO.manuscriptLogin.MSID));
                manuscriptLoginDTO.manuscriptLogin.RevisionParentId = _manuscriptLoginDBRepositoryReadSide.GetParentCrestId(manuscriptLoginDTO.manuscriptLogin.MSID);
                manuscriptLoginDTO.manuscriptLogin.MSID = manuscriptLoginDTO.manuscriptLogin.MSID + "_R" + Convert.ToString(_manuscriptLoginDBRepositoryReadSide.GetRevisionCount(manuscriptLoginDTO.manuscriptLogin.MSID));
            }

            ManuscriptLoginUnitOfWork manuscriptLoginUnitOfWork = null;
            try
            {
                manuscriptLoginUnitOfWork = new ManuscriptLoginUnitOfWork(conString);
                manuscriptLoginUnitOfWork.manuscriptLoginDTO = manuscriptLoginDTO;
                manuscriptLoginUnitOfWork.SaveManuscriptLogin();
                SaveManuscriptDetails(manuscriptLoginDTO, manuscriptStatusId, manuscriptLoginUnitOfWork);

                return true;
            }
            catch (Exception ex)
            {
                
            }
            //exception will be raised up in the call stack
            finally
            {
                if (manuscriptLoginUnitOfWork != null)
                {
                    manuscriptLoginUnitOfWork.Dispose();
                }
            }
            return true;
        }

        private void SaveManuscriptDetails(ManuscriptLoginDTO manuscriptLoginDTO, int manuscriptStatusID, ManuscriptLoginUnitOfWork _manuscriptLoginUnitOfWork)
        {
            int crestID = _manuscriptLoginDBRepositoryReadSide.GetCrestID(manuscriptLoginDTO.manuscriptLogin.MSID);
            manuscriptLoginDTO.IsCrestIDPresent = _manuscriptLoginDBRepositoryReadSide.IsCrestIDPresent(crestID);
            bool IsBoth = _manuscriptLoginDBRepositoryReadSide.IsServiceTypeBoth(manuscriptLoginDTO.manuscriptLogin.ServiceTypeStatusId);
            if (IsBoth)
            {
                var msLoginDetails = new ManuscriptLoginDetails
                {
                    AssignedDate = DateTime.Now,
                    ServiceTypeStatusId = _manuscriptLoginDBRepositoryReadSide.MSServiceTypeID(),
                    JobStatusId = Convert.ToInt32(manuscriptStatusID),
                    CrestId = crestID,
                    RoleId = _manuscriptLoginDBRepositoryReadSide.GetAssociateRole(),
                    JobProcessStatusId = _manuscriptLoginDBRepositoryReadSide.GetStatusMaster().Where(x => x.Description.ToLower() == "open").Select(x => x.ID).FirstOrDefault()
                };
                if (!string.IsNullOrEmpty(manuscriptLoginDTO.AssociateName))
                {
                    var prGetAssociateInfoResult =
                        _manuscriptDBRepositoryReadSide.GetAssociateName(manuscriptLoginDTO.AssociateName)
                            .FirstOrDefault();
                    msLoginDetails.UserRoleId = prGetAssociateInfoResult.ID;
                    msLoginDetails.JobProcessStatusId = _manuscriptLoginDBRepositoryReadSide.GetStatusMaster().Where(x => x.Description.ToLower() == "assigned").Select(x => x.ID).FirstOrDefault();
                }
                    manuscriptLoginDTO.manuscriptLoginDetails.Add(msLoginDetails);
                if (manuscriptLoginDTO.IsCrestIDPresent)
                {
                    var rsLoginDetails = new ManuscriptLoginDetails
                    {
                        AssignedDate = DateTime.Now,
                        CrestId = crestID,
                        JobStatusId = Convert.ToInt32(manuscriptStatusID),
                        RoleId = _manuscriptLoginDBRepositoryReadSide.GetAssociateRole(),
                        ServiceTypeStatusId = _manuscriptLoginDBRepositoryReadSide.RSServiceTypeID(),
                        JobProcessStatusId = _manuscriptLoginDBRepositoryReadSide.GetStatusMaster().Where(x => x.Description.ToLower() == "open").Select(x => x.ID).FirstOrDefault()
                    };
                    manuscriptLoginDTO.manuscriptLoginDetails.Add(rsLoginDetails);

                    //update record
                    var updateManuscriptLoginDetails = new ManuscriptLoginDetails();
                    updateManuscriptLoginDetails = _manuscriptLoginDBRepositoryReadSide.GetManuscriptLoginDetails(crestID, _manuscriptLoginDBRepositoryReadSide.GetServiceTypeStatusId(crestID));
                    updateManuscriptLoginDetails.SubmitedDate = DateTime.Now;
                    updateManuscriptLoginDetails.FetchedDate = DateTime.Now;
                    //update using query 
                    updateManuscriptLoginDetails.JobProcessStatusId = _manuscriptLoginDBRepositoryReadSide.GetStatusMaster().Where(x => x.Description.ToLower() == "unallocate").Select(x => x.ID).FirstOrDefault();
                    //update using query
                    updateManuscriptLoginDetails.JobStatusId = _manuscriptLoginDBRepositoryReadSide.GetStatusMaster().Where(x => x.Description.ToLower() == "close").Select(x => x.ID).FirstOrDefault();
                    manuscriptLoginDTO.manuscriptLoginDetails.Add(updateManuscriptLoginDetails);

                    if (_manuscriptLoginDetailsRepository.GetOpenManuscriptCount(crestID) == 2)
                    {
                        var updateManuscriptLoginDetailsRS = _manuscriptLoginDBRepositoryReadSide.GetManuscriptLoginDetails(crestID, _manuscriptLoginDBRepositoryReadSide.RSServiceTypeID());
                        updateManuscriptLoginDetailsRS.SubmitedDate = DateTime.Now;
                        updateManuscriptLoginDetailsRS.FetchedDate = DateTime.Now;
                        //update using query
                        updateManuscriptLoginDetailsRS.JobProcessStatusId = _manuscriptLoginDBRepositoryReadSide.GetStatusMaster().Where(x => x.Description.ToLower() == "unallocate").Select(x => x.ID).FirstOrDefault();
                        //update using query
                        updateManuscriptLoginDetailsRS.JobStatusId = _manuscriptLoginDBRepositoryReadSide.GetStatusMaster().Where(x => x.Description.ToLower() == "close").Select(x => x.ID).FirstOrDefault();
                        manuscriptLoginDTO.manuscriptLoginDetails.Add(updateManuscriptLoginDetailsRS);
                    }
                }

                if (!manuscriptLoginDTO.IsCrestIDPresent)
                {
                    var rsLoginDetails = new ManuscriptLoginDetails
                    {
                        AssignedDate = DateTime.Now,
                        CrestId = crestID,
                        JobStatusId = Convert.ToInt32(manuscriptStatusID),
                        RoleId = _manuscriptLoginDBRepositoryReadSide.GetAssociateRole(),
                        ServiceTypeStatusId = _manuscriptLoginDBRepositoryReadSide.RSServiceTypeID(),
                        JobProcessStatusId = _manuscriptLoginDBRepositoryReadSide.GetStatusMaster().Where(x => x.Description.ToLower() == "open").Select(x => x.ID).FirstOrDefault()
                    };
                    manuscriptLoginDTO.manuscriptLoginDetails.Add(rsLoginDetails);
                }
            }
            else if (_manuscriptLoginDetailsRepository.GetOpenManuscriptCount(crestID)==2)
            {
                var msLoginDetails = new ManuscriptLoginDetails
                {
                    AssignedDate = DateTime.Now,
                    ServiceTypeStatusId = manuscriptLoginDTO.manuscriptLogin.ServiceTypeStatusId,
                    JobStatusId = Convert.ToInt32(manuscriptStatusID),
                    CrestId = crestID,
                    RoleId = _manuscriptLoginDBRepositoryReadSide.GetAssociateRole(),
                    JobProcessStatusId = _manuscriptLoginDBRepositoryReadSide.GetStatusMaster().Where(x => x.Description.ToLower() == "open").Select(x => x.ID).FirstOrDefault()
                };
                if (!string.IsNullOrEmpty(manuscriptLoginDTO.AssociateName))
                {
                    var prGetAssociateInfoResult =_manuscriptDBRepositoryReadSide.GetAssociateName(manuscriptLoginDTO.AssociateName).FirstOrDefault();
                    msLoginDetails.UserRoleId = prGetAssociateInfoResult.ID;
                    msLoginDetails.JobProcessStatusId = _manuscriptLoginDBRepositoryReadSide.GetStatusMaster().Where(x => x.Description.ToLower() == "assigned").Select(x => x.ID).FirstOrDefault();
                }



                manuscriptLoginDTO.manuscriptLoginDetails.Add(msLoginDetails);
                if (manuscriptLoginDTO.IsCrestIDPresent)
                {
                    //update record
                    var updateManuscriptLoginDetailsMS= new ManuscriptLoginDetails();
                    updateManuscriptLoginDetailsMS = _manuscriptLoginDBRepositoryReadSide.GetManuscriptLoginDetails(crestID, _manuscriptLoginDBRepositoryReadSide.MSServiceTypeID());
                    updateManuscriptLoginDetailsMS.SubmitedDate = DateTime.Now;
                    updateManuscriptLoginDetailsMS.FetchedDate = DateTime.Now;
                    //update using query 
                    updateManuscriptLoginDetailsMS.JobProcessStatusId = _manuscriptLoginDBRepositoryReadSide.GetStatusMaster().Where(x => x.Description.ToLower() == "unallocate").Select(x => x.ID).FirstOrDefault();
                    //update using query
                    updateManuscriptLoginDetailsMS.JobStatusId = _manuscriptLoginDBRepositoryReadSide.GetStatusMaster().Where(x => x.Description.ToLower() == "close").Select(x => x.ID).FirstOrDefault();
                    manuscriptLoginDTO.manuscriptLoginDetails.Add(updateManuscriptLoginDetailsMS);

                    //update record  _manuscriptLoginDetailsRepository
                    var updateManuscriptLoginDetailsRS = _manuscriptLoginDBRepositoryReadSide.GetManuscriptLoginDetails(crestID, _manuscriptLoginDBRepositoryReadSide.RSServiceTypeID());
                    updateManuscriptLoginDetailsRS.SubmitedDate = DateTime.Now;
                    updateManuscriptLoginDetailsRS.FetchedDate = DateTime.Now;
                    //update using query
                    updateManuscriptLoginDetailsRS.JobProcessStatusId = _manuscriptLoginDBRepositoryReadSide.GetStatusMaster().Where(x => x.Description.ToLower() == "unallocate").Select(x => x.ID).FirstOrDefault();
                    //update using query
                    updateManuscriptLoginDetailsRS.JobStatusId = _manuscriptLoginDBRepositoryReadSide.GetStatusMaster().Where(x => x.Description.ToLower() == "close").Select(x => x.ID).FirstOrDefault();
                    manuscriptLoginDTO.manuscriptLoginDetails.Add(updateManuscriptLoginDetailsRS);


                }
            }
            else
            {
                var manuscriptLoginDetails = new ManuscriptLoginDetails
                {
                    AssignedDate = DateTime.Now,
                    CrestId = crestID,
                    ServiceTypeStatusId = manuscriptLoginDTO.manuscriptLogin.ServiceTypeStatusId,
                    JobStatusId = Convert.ToInt32(manuscriptStatusID),
                    RoleId = _manuscriptLoginDBRepositoryReadSide.GetAssociateRole(),
                    JobProcessStatusId = _manuscriptLoginDBRepositoryReadSide.GetStatusMaster().Where(x => x.Description.ToLower() == "open").Select(x => x.ID).FirstOrDefault()
                };
                if (!string.IsNullOrEmpty(manuscriptLoginDTO.AssociateName))
                {
                    var prGetAssociateInfoResult =
                        _manuscriptDBRepositoryReadSide.GetAssociateName(manuscriptLoginDTO.AssociateName)
                            .FirstOrDefault();
                    manuscriptLoginDetails.UserRoleId = prGetAssociateInfoResult.ID;
                    manuscriptLoginDetails.JobProcessStatusId = _manuscriptLoginDBRepositoryReadSide.GetStatusMaster().Where(x => x.Description.ToLower() == "assigned").Select(x => x.ID).FirstOrDefault();
                }
                manuscriptLoginDTO.manuscriptLoginDetails.Add(manuscriptLoginDetails);
                if (manuscriptLoginDTO.IsCrestIDPresent)
                {
                    //update record  _manuscriptLoginDetailsRepository
                    var updateManuscriptLoginDetailsMS = _manuscriptLoginDBRepositoryReadSide.GetManuscriptLoginDetails(crestID, _manuscriptLoginDBRepositoryReadSide.GetServiceTypeStatusId(crestID));
                    updateManuscriptLoginDetailsMS.SubmitedDate = DateTime.Now;
                    updateManuscriptLoginDetailsMS.FetchedDate = DateTime.Now;
                    //update using query
                    updateManuscriptLoginDetailsMS.JobProcessStatusId = _manuscriptLoginDBRepositoryReadSide.GetStatusMaster().Where(x => x.Description.ToLower() == "unallocate").Select(x => x.ID).FirstOrDefault();
                    //update using query
                    updateManuscriptLoginDetailsMS.JobStatusId = _manuscriptLoginDBRepositoryReadSide.GetStatusMaster().Where(x => x.Description.ToLower() == "close").Select(x => x.ID).FirstOrDefault();
                    manuscriptLoginDTO.manuscriptLoginDetails.Add(updateManuscriptLoginDetailsMS);
                }
            }
            _manuscriptLoginUnitOfWork.SaveManuscriptLoginDetails();
        }

        public bool ImpersonateUser()
        {
            impersonate = new Impersonation.Impersonate();
            pr_ImpersonateCredential_Result impersonateCredential = new pr_ImpersonateCredential_Result();
            ManuscriptLoginDBRepositoryReadSide ManuscriptLoginDBRepositoryReadSide = new ManuscriptLoginDBRepositoryReadSide(conString);
            impersonateCredential = ManuscriptLoginDBRepositoryReadSide.GetImpersonateCredential();
            return impersonate.StartImpersonation(impersonateCredential.ServerUserName, impersonateCredential.Domain, impersonateCredential.Password);
        }

        public IDictionary<string, string> CreateJournalFolderStructure(string fileServerIPPath, string manuscriptFilePath, IDictionary<string, string> dataErrors, ManuscriptLoginDTO manuscriptLoginDTO)
        {
            if (ImpersonateUser())
            {
                //Impersonation.Impersonate impersonate = new Impersonation.Impersonate();
                List<Journal> journalList = new List<Journal>();
                //ManuscriptLogin manuscriptLogin = new ManuscriptLogin();
                _manuscriptDBRepositoryReadSide = new ManuscriptDBRepositoryReadSide(conString);
                string journalFolderPath = string.Empty;
                string fileName = manuscriptFilePath.Substring(manuscriptFilePath.LastIndexOf("\\"));

                string revisionNo = string.Empty;
                journalList = _manuscriptDBRepositoryReadSide.GetJournalList();
                revisionNo = Convert.ToString(_manuscriptLoginDBRepositoryReadSide.GetRevisionCount(manuscriptLoginDTO.manuscriptLogin.MSID));
                var journalName = (from journal in journalList
                                   where journal.ID == manuscriptLoginDTO.manuscriptLogin.JournalId
                                   select journal.JournalTitle).FirstOrDefault();

                journalFolderPath = fileServerIPPath + "\\" + journalName + "\\" + manuscriptLoginDTO.manuscriptLogin.MSID + "\\Manuscript";
                if (manuscriptLoginDTO.IsRevision)
                    journalFolderPath = fileServerIPPath + "\\" + journalName + "\\" + manuscriptLoginDTO.manuscriptLogin.MSID + "\\Manuscript\\Revision " + revisionNo;
                else
                    journalFolderPath = fileServerIPPath + "\\" + journalName + "\\" + manuscriptLoginDTO.manuscriptLogin.MSID + "\\Manuscript\\Fresh";
                if (CreateDirectory(journalFolderPath))
                {
                    File.Copy(manuscriptFilePath, journalFolderPath + fileName, true);
                    File.Delete(manuscriptFilePath);
                    manuscriptLoginDTO.manuscriptLogin.ManuscriptFilePath = journalFolderPath;
                    impersonate.EndImpersonation();
                }
                else
                {
                    dataErrors.Add("Impersonation Error", "File is not uploaded file server.");
                    return dataErrors;
                }
            }
            else
            {
                dataErrors.Add("Impersonation Error", "File is not uploaded file server.");
                return dataErrors;
            }
            return dataErrors;
        }

        public bool CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
            if (Directory.Exists(path))
            {
                return true;
            }
            return false;
        }
    }
}
