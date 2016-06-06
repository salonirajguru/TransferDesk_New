using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using System.Web.UI.WebControls;
using Microsoft.Ajax.Utilities;
using TransferDesk.Contracts.Manuscript.ComplexTypes.UserRole;
using TransferDesk.Contracts.Manuscript.Entities;
using TransferDesk.DAL.Manuscript.Repositories;
using TransferDesk.Services.Manuscript;
using TransferDesk.Services.Manuscript.ViewModel;

namespace TransferDesk.MS.Web.Controllers
{
    public class ManuscriptLoginController : Controller
    {
        private readonly ManuscriptDBRepositoryReadSide _manuscriptDBRepositoryReadSide;
        private ManuscriptLoginDBRepositoryReadSide ManuscriptLoginDbRepositoryReadSide { get; set; }
        private readonly ManuscriptLoginService _manuscriptLoginService;
        private readonly string _conString;

        public ManuscriptLoginController()
        {
            _conString = Convert.ToString(ConfigurationManager.AppSettings["dbTransferDeskService"]);
            _manuscriptDBRepositoryReadSide = new ManuscriptDBRepositoryReadSide(_conString);
            ManuscriptLoginDbRepositoryReadSide = new ManuscriptLoginDBRepositoryReadSide(_conString);
            _manuscriptLoginService = new ManuscriptLoginService(_conString);
        }

        // GET: Login
        [HttpGet]
        public ActionResult ManuscriptLogin(int? id)
        {
            var manuscriptLoginVm = new ManuscriptLoginVM();
            var userId = @System.Web.HttpContext.Current.User.Identity.Name.Replace("SPRINGER-SBM\\", "");
            manuscriptLoginVm._journal = _manuscriptDBRepositoryReadSide.GetJournalList();
            var crestId = Convert.ToInt32(id);
            if (id != null && id != 0)
            {
                ManuscriptLoginVmDetails(manuscriptLoginVm, crestId);
            }
            manuscriptLoginVm._articleType = _manuscriptDBRepositoryReadSide.GetArticleList(Convert.ToInt32(manuscriptLoginVm.JournalID));
            manuscriptLoginVm._section = _manuscriptDBRepositoryReadSide.GetSectionList(Convert.ToInt32(manuscriptLoginVm.JournalID));
            manuscriptLoginVm._TaskList = _manuscriptDBRepositoryReadSide.GetTaskType();
            manuscriptLoginVm._serviceType = _manuscriptDBRepositoryReadSide.GetServiceType();
            manuscriptLoginVm.EmployeeName = _manuscriptDBRepositoryReadSide.EmployeeName(userId);
            manuscriptLoginVm.manuscriptLoginedJobs = ManuscriptLoginDbRepositoryReadSide.GetManuscriptLoginJobs();
            return View(manuscriptLoginVm);
        }

        private void ManuscriptLoginVmDetails(ManuscriptLoginVM manuscriptLoginVm, int crestId)
        {
            ManuscriptLogin manuscriptLogin;
            var manuscriptLoginDetails = new ManuscriptLoginDetails();
            manuscriptLogin = ManuscriptLoginDbRepositoryReadSide.GetManuscriptByCrestID(crestId);
            manuscriptLoginVm.CrestId = manuscriptLogin.CrestId;
            manuscriptLoginVm.SpecialInstruction = manuscriptLogin.SpecialInstruction;
            //change as per id
            if (manuscriptLogin.ServiceTypeStatusId == 9)
            {
                manuscriptLoginDetails = ManuscriptLoginDbRepositoryReadSide.GetManuscriptLoginDetails(manuscriptLogin.CrestId, ManuscriptLoginDbRepositoryReadSide.MSServiceTypeID());
            }
            else
            {
                manuscriptLoginDetails = ManuscriptLoginDbRepositoryReadSide.GetManuscriptLoginDetails(manuscriptLogin.CrestId, manuscriptLogin.ServiceTypeStatusId);
            }
            if (manuscriptLoginDetails != null)
            {
                if (manuscriptLoginDetails.UserRoleId != null && manuscriptLoginDetails.UserRoleId != 0)
                {
                    var usernameID = ManuscriptLoginDbRepositoryReadSide.GetUserID(Convert.ToInt32(manuscriptLoginDetails.UserRoleId)).UserID;
                    manuscriptLoginVm.Associate = _manuscriptDBRepositoryReadSide.EmployeeName(usernameID);
                }
            }
            manuscriptLoginVm.InitialSubmissionDate = manuscriptLogin.InitialSubmissionDate;
            manuscriptLoginVm.ManuscriptFilePath = manuscriptLogin.ManuscriptFilePath;
            manuscriptLoginVm.ServiceTypeID = manuscriptLogin.ServiceTypeStatusId;
            manuscriptLoginVm.ArticleTypeID = manuscriptLogin.ArticleTypeId;
            manuscriptLoginVm.JournalID = manuscriptLogin.JournalId;
            manuscriptLoginVm.MSID = manuscriptLogin.MSID;
            manuscriptLoginVm.SectionID = manuscriptLogin.SectionId;
            manuscriptLoginVm.ArticleTitle = manuscriptLogin.ArticleTitle;
            manuscriptLoginVm.ReceivedDate = manuscriptLogin.ReceivedDate;
            manuscriptLoginVm.TaskID = manuscriptLogin.TaskID;
        }

        [HttpPost]
        public ActionResult ManuscriptLogin(ManuscriptLoginVM manuscriptLoginVm, HttpPostedFileBase manuscriptFilePath)
        {
            manuscriptLoginVm.MSID = manuscriptLoginVm.MSID.Trim();
            IDictionary<string, string> dataErrors = new Dictionary<string, string>();
            if (!String.IsNullOrEmpty(manuscriptLoginVm.Associate))
            {
                var empInfo = _manuscriptDBRepositoryReadSide.GetAssociateName(manuscriptLoginVm.Associate);
                if (empInfo.Count() > 0)
                    manuscriptLoginVm.userID = empInfo.FirstOrDefault().UserID;
            }
            if (manuscriptLoginVm.CrestId == 0)
            {
                if (manuscriptLoginVm.IsRevision)
                {
                    //SaveTempFileOnIISServer(manuscriptLoginVm, manuscriptFilePath);
                    AddManuscriptLoginInfo(manuscriptLoginVm, dataErrors);
                }
                else
                {
                    if (!ManuscriptLoginDbRepositoryReadSide.IsMSIDAvailable(manuscriptLoginVm.MSID, manuscriptLoginVm.CrestId))
                        TempData["MSIDError"] = "<script>alert('Manuscript Number is already present.');</script>";
                    else
                    {
                        //SaveTempFileOnIISServer(manuscriptLoginVm, manuscriptFilePath);
                        AddManuscriptLoginInfo(manuscriptLoginVm, dataErrors);
                    }
                }
            }
            else
            {
                if (manuscriptLoginVm.IsRevision)
                {
                    //SaveTempFileOnIISServer(manuscriptLoginVm, manuscriptFilePath);
                    AddManuscriptLoginInfo(manuscriptLoginVm, dataErrors);
                }
                else
                {
                    if (ManuscriptLoginDbRepositoryReadSide.IsMSIDAvailable(manuscriptLoginVm.MSID, manuscriptLoginVm.CrestId))
                        TempData["MSIDError"] = "<script>alert('Manuscript Number is already present.');</script>";
                    else
                    {
                        var manuscriptLogin = new ManuscriptLogin();
                        manuscriptLogin = ManuscriptLoginDbRepositoryReadSide.GetManuscriptByCrestID(manuscriptLoginVm.CrestId);
                        //code to updated record
                        _manuscriptLoginService.SaveManuscriptLoginVM(dataErrors, manuscriptLoginVm, manuscriptLogin);
                        TempData["msg"] = "<script>alert('Record updated succesfully');</script>";
                    }
                }
            }
            return RedirectToAction("ManuscriptLogin", new { id = 0 });
        }

        private void SaveTempFileOnIISServer(ManuscriptLoginVM manuscriptLoginVm, HttpPostedFileBase manuscriptFilePath)
        {
            if (!String.IsNullOrEmpty(manuscriptLoginVm.ManuscriptFilePath))
            {
                var path = Path.Combine(Server.MapPath("~/App_Data/"), manuscriptFilePath.FileName);
                manuscriptFilePath.SaveAs(path);
                manuscriptLoginVm.ManuscriptFilePath = path;
            }
            else
                manuscriptLoginVm.ManuscriptFilePath = "";
        }

        private void AddManuscriptLoginInfo(ManuscriptLoginVM manuscriptLoginVm, IDictionary<string, string> dataErrors)
        {
            //code to add record
            var manuscriptLogin = new ManuscriptLogin();
            //if new record or revision then add entry into db
            manuscriptLoginVm.CrestId = 0;
            _manuscriptLoginService.SaveManuscriptLoginVM(dataErrors, manuscriptLoginVm, manuscriptLogin);
            TempData["msg"] = "<script>alert('Record added succesfully');</script>";
        }

        [HttpPost]
        public ActionResult _BookInfo()
        {
            return RedirectToAction("ManuscriptLogin");
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public JsonResult GetAssociateName(string searchText)
        {
            return this.Json(_manuscriptDBRepositoryReadSide.GetAssociateName(searchText), JsonRequestBehavior.AllowGet);
        }

        public void SendMail(Dictionary<string, string> dicReplace, string emailTemplatePath, string emailSubject, string emailFrom, string emailTo, string emailCC, string emailBCC)
        {
            var reviewerService = new ReviewerService(_conString, _conString);
            emailTemplatePath = emailTemplatePath;
            var emailBody = new StringBuilder(System.IO.File.ReadAllText(emailTemplatePath));
            foreach (var kvp in dicReplace)
            {
                emailBody.Replace(kvp.Key, kvp.Value);
            }
            var objEmail = new Email();
            objEmail.SendEmail(emailTo, emailFrom, emailCC, emailBCC, emailSubject, Convert.ToString(emailBody));

            //save mail details
            reviewerService.SaveMailDetails(dicReplace, emailTo, emailFrom, emailCC, emailBCC, emailSubject, Convert.ToString(emailBody));
        }

        public bool ValidateMsidIsOpen(string msid)
        {
            return ManuscriptLoginDbRepositoryReadSide.IsMsidOpen(msid);
        }

        public bool IsMsidAvaialable(string msid)
        {
            return ManuscriptLoginDbRepositoryReadSide.IsMSIDAvailable(msid, 0);
        }

        public string GetJournalLink(int journalId)
        {
            return _manuscriptDBRepositoryReadSide.GetJournalList().Find(x => x.ID == journalId).Link;
        }

        public ActionResult ManuscriptLoginExportResult(string FromDate, string ToDate)
        {
            if (FromDate == "" || ToDate == "")
            {
                TempData["msg"] = "<script>alert('Please select Date');</script>";
                return RedirectToAction("ManuscriptLogin");
            }

            var grid = new GridView();
            var manuscriptLoginExportJobs = ManuscriptLoginDbRepositoryReadSide.GetManuscriptLoginJobsDetailsForExcel(FromDate, ToDate);
            var countData = manuscriptLoginExportJobs.Count();
            if (countData > 0)
            {
                grid.DataSource = manuscriptLoginExportJobs;
                grid.DataBind();
                grid.HeaderStyle.Font.Bold = true;
                grid.HeaderRow.BackColor = System.Drawing.Color.LightGray;
                grid.HeaderRow.Cells[0].Text = "Crest ID";
                grid.HeaderRow.Cells[1].Text = "Service Type";
                grid.HeaderRow.Cells[2].Text = "Journal Title";
                grid.HeaderRow.Cells[3].Text = "Manuscript Number";
                grid.HeaderRow.Cells[4].Text = "Article Type";
                grid.HeaderRow.Cells[5].Text = "Section";
                grid.HeaderRow.Cells[6].Text = "Journal Link";
                grid.HeaderRow.Cells[7].Text = "Article Title";
                grid.HeaderRow.Cells[8].Text = "Special Instruction";
                grid.HeaderRow.Cells[9].Text = "Associate";
                grid.HeaderRow.Cells[10].Text = "Initial Submission Date";
                Response.ClearContent();
                Response.Buffer = true;
                Response.AddHeader("content-disposition",string.Format("attachment; filename={0}", "ManuscriptLogin" + DateTime.Now.ToShortDateString() + ".xls"));
                Response.ContentType = "application/ms-excel";
                StringWriter sw = new StringWriter();
                HtmlTextWriter htw = new HtmlTextWriter(sw);
                grid.RenderControl(htw);
                Response.Write(sw.ToString());
                Response.Flush();
                Response.End();
                return View();
            }
            else
            {
                TempData["msg"] = "<script>alert('No Record Found');</script>";
                return RedirectToAction("ManuscriptLogin");
            }

        }
    }
}