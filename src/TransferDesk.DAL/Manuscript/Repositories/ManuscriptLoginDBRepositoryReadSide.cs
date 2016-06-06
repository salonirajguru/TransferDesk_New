using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RepositoryInterfaces = TransferDesk.Contracts.Manuscript.Repositories;
using Entities = TransferDesk.Contracts.Manuscript.Entities;

using DataContexts = TransferDesk.DAL.Manuscript.DataContext;
using TransferDesk.Contracts.Manuscript.ComplexTypes.Search;
using System.Data.Entity.Core.Objects;
using System.Data.SqlClient;
using TransferDesk.Contracts.Manuscript.ComplexTypes.UserRole;
using System.Collections;
using TransferDesk.Contracts.Manuscript.ComplexTypes.ManuscriptLogin;
using System.Globalization;

namespace TransferDesk.DAL.Manuscript.Repositories
{
    public class ManuscriptLoginDBRepositoryReadSide : IDisposable
    {
        public DataContexts.ManuscriptDBContext manuscriptDataContextRead;

        public ManuscriptLoginDBRepositoryReadSide(string conString)
        {
            this.manuscriptDataContextRead = new DataContexts.ManuscriptDBContext(conString);
        }

        public int GetCrestID(string msid)
        {
            var crestId = (from q in manuscriptDataContextRead.ManuscriptLogin
                           where q.MSID == msid
                           select q.CrestId).FirstOrDefault();
            return crestId;
        }

        public int GetParentCrestId(string msid)
        {
            var parentCrestId = (from q in manuscriptDataContextRead.ManuscriptLogin
                                 where q.MSID == msid
                                 select q.CrestId).FirstOrDefault();
            var revisionParentCrestId = (from q in manuscriptDataContextRead.ManuscriptLogin
                                         where q.MSID.Contains(msid + "_R")
                                         orderby q.CrestId descending
                                         select q.CrestId).FirstOrDefault();
            if (revisionParentCrestId != 0)
                return revisionParentCrestId;
            else
            {
                return parentCrestId;
            }

        }


        public bool IsServiceTypeBoth(int serviceTypeID)
        {
            int count = (from q in manuscriptDataContextRead.StatusMaster
                         where q.ID == serviceTypeID && q.Description.ToLower() == "both"
                         select q).ToList().Count;
            if (count > 0)
                return true;
            else
                return false;
        }

        public int MSServiceTypeID()
        {
            int msServiceTypeID = (from q in manuscriptDataContextRead.StatusMaster
                                   where q.Description.ToLower() == "Manuscript Screening"
                                   select q.ID).FirstOrDefault();
            return msServiceTypeID;
        }

        public int RSServiceTypeID()
        {
            var rsServiceTypeID = (from q in manuscriptDataContextRead.StatusMaster
                                   where q.Description.ToLower() == "Reviewer Suggestion"
                                   select q.ID).FirstOrDefault();
            return rsServiceTypeID;
        }

        public int GetAssociateRole()
        {
            int associateRoleID = (from q in manuscriptDataContextRead.Roles
                                   where q.RoleName.ToLower() == "associate"
                                   select q.ID).FirstOrDefault();
            return associateRoleID;
        }

        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    manuscriptDataContextRead.Dispose();
                }
            }
            this.disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public int GetRevisionCount(string msid)
        {
            var result = (from q in manuscriptDataContextRead.ManuscriptLogin
                          where q.MSID.Contains(msid + "_R")
                          orderby q.Revision descending
                          select q.Revision).ToList();
            var revisionCount = 0;
            if (result.Count() > 0)
                revisionCount = Convert.ToInt32(result.First());

            return (revisionCount + 1);
        }

        public bool IsMSIDAvailable(string msid, int id)
        {
            if (id == 0)
            {
                var result = from q in manuscriptDataContextRead.ManuscriptLogin
                             where q.MSID == msid
                             select q;
                if (result.ToList().Count() == 0)
                    return true;
                else
                    return false;
            }
            else
            {
                var result = from q in manuscriptDataContextRead.ManuscriptLogin
                             where q.MSID == msid
                             select q;
                var count = result.ToList().Count();
                if (result.ToList().Count() == 1)
                {
                    var pkCheck = from manuscript in result
                                  where manuscript.CrestId == id
                                  select manuscript;
                    if (pkCheck.ToList().Count() == 1)
                        return false;
                    else
                        return true;
                }
                else if (result.ToList().Count() > 1)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool IsCrestIDPresent(int crestID)
        {
            int IsCrestIDPresent = (from q in manuscriptDataContextRead.ManuscriptLoginDetails
                                    where q.CrestId == crestID && q.JobStatusId == 7
                                    select q.CrestId).ToList().Count;
            if (IsCrestIDPresent > 0)
                return true;
            else
                return false;
        }

        public int GetManuscriptLoginDetailsID(int crestID, int ServiceTypeStatusId, int JobStatusId)
        {
            int id = (from q in manuscriptDataContextRead.ManuscriptLoginDetails
                      where q.CrestId == crestID && q.ServiceTypeStatusId == ServiceTypeStatusId && q.JobStatusId == JobStatusId
                      orderby q.Id descending
                      select q.Id).FirstOrDefault();
            return id;
        }

        public Entities.ManuscriptLoginDetails GetManuscriptLoginDetails(int crestID, int ServiceTypeStatusId)
        {
            var manuscriptLoginDetails = (from q in manuscriptDataContextRead.ManuscriptLoginDetails
                                          where q.CrestId == crestID && q.ServiceTypeStatusId == ServiceTypeStatusId && q.JobStatusId == 7
                                          select q).FirstOrDefault();
            return manuscriptLoginDetails;
        }
        public int GetServiceTypeStatusId(int crestId)
        {
            var serviceTypeStatusId = (from q in manuscriptDataContextRead.ManuscriptLoginDetails
                                       where q.CrestId == crestId && q.JobStatusId == 7
                                       select q.ServiceTypeStatusId).FirstOrDefault();
            return serviceTypeStatusId;
        }

        public Entities.ManuscriptLoginDetails GetManuscriptLoginDetails(int crestID, int ServiceTypeStatusId, int JobStatusId)
        {
            var manuscriptLoginDetails = (from q in manuscriptDataContextRead.ManuscriptLoginDetails
                                          where q.CrestId == crestID && q.ServiceTypeStatusId == ServiceTypeStatusId
                                          orderby q.Id descending
                                          select q).FirstOrDefault();
            return manuscriptLoginDetails;
        }

        public List<Entities.StatusMaster> GetStatusMaster()
        {
            var statusMaster = manuscriptDataContextRead.StatusMaster.ToList();
            return statusMaster;
        }

        public List<pr_GetManuscriptLoginJobs_Result> GetManuscriptLoginJobs()
        {
            try
            {
                List<pr_GetManuscriptLoginJobs_Result> manuscriptLoginJobs =
             this.manuscriptDataContextRead.Database.SqlQuery<pr_GetManuscriptLoginJobs_Result>("exec pr_GetManuscriptLoginJobs").ToList();
                return manuscriptLoginJobs;
            }
            catch
            {
                return null;//todo:check and remove this trycatchhandler
            }
            finally
            {

            }

        }

        public Entities.ManuscriptLogin GetManuscriptByCrestID(int crestid)
        {
            var manuscriptLogin = manuscriptDataContextRead.ManuscriptLogin.Find(crestid);
            return manuscriptLogin;
        }

        public Entities.UserRoles GetUserID(int userRoleID)
        {
            var userRole = manuscriptDataContextRead.UserRoles.Find(userRoleID);
            return userRole;
        }

        public pr_ImpersonateCredential_Result GetImpersonateCredential()
        {

            pr_ImpersonateCredential_Result impersonateCredential =
            this.manuscriptDataContextRead.Database.SqlQuery<pr_ImpersonateCredential_Result>("exec pr_ImpersonateCredential").ToList().FirstOrDefault();

            return impersonateCredential;
        }

        public bool IsMsidOpen(string msid)
        {
            var id = (from q in manuscriptDataContextRead.ManuscriptLogin
                      where (q.MSID == msid || q.MSID.Contains(msid + "_R")) && q.ManuscriptStatusId == 7
                      select q.MSID).FirstOrDefault();
            if (id == null || id == "0")
                return false;
            else
                return true;
        }

        public List<pr_GetManuscriptLoginExportJobs_Result> GetManuscriptLoginJobsDetailsForExcel(string FromDate1, string ToDate1)
        {
            try
            {
                var FromDate = Convert.ToDateTime(FromDate1);               
                var ToDate = Convert.ToDateTime(ToDate1);
                var FromDateParameter = FromDate != null ?
                new SqlParameter("FromDate", FromDate) :
                new SqlParameter("FromDate", typeof(global::System.DateTime));

                var ToDateParameter = ToDate != null ?
                new SqlParameter("ToDate", ToDate) :
                new SqlParameter("ToDate", typeof(global::System.DateTime));

                List<pr_GetManuscriptLoginJobs_Result> manuscriptLoginJobsForExcel =
                    this.manuscriptDataContextRead.Database.SqlQuery<pr_GetManuscriptLoginJobs_Result>("pr_LoginExportToExcel @FromDate, @ToDate", FromDateParameter, ToDateParameter).ToList();
                var manuscriptLoginExportJobs = (from q in manuscriptLoginJobsForExcel
                              select new pr_GetManuscriptLoginExportJobs_Result()
                              {
                                 CrestId=q.CrestId,
                                 ServiceType=q.ServiceType,
                                 JournalTitle=q.JournalTitle,
                                 MSID=q.MSID,
                                 ArticleTypeName=q.ArticleTypeName,
                                 SectionName=q.SectionName,
                                 Link=q.Link,
                                 ArticleTitle=q.ArticleTitle,
                                 SpecialInstruction=q.SpecialInstruction,
                                 Associate=q.Associate,
                                  InitialSubmissionDate = q.InitialSubmissionDate.ToString("dd/MM/yyyy")
                              }).ToList<pr_GetManuscriptLoginExportJobs_Result>();
                return manuscriptLoginExportJobs;
            }
            catch
            {
                return null;//todo:check and remove this trycatchhandler
            }
            finally
            {

            }

        }
    }
}
