using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//Data Access Layer
using Repos = TransferDesk.DAL.Manuscript.Repositories;
using DContext = TransferDesk.DAL.Manuscript.DataContext;

//Contracts
using Entities = TransferDesk.Contracts.Manuscript.Entities;
using DTOs = TransferDesk.Contracts.Manuscript.DTO;
namespace TransferDesk.DAL.Manuscript.UnitOfWork
{
    public class ManuscriptLoginUnitOfWork:IDisposable
    {

        private Repos.ManuscriptLoginRepository _manuscriptLoginRepository;
        private Repos.ManuscriptLoginDetailsRepository _manuscriptLoginDetailsRepository;

        public DTOs.ManuscriptLoginDTO manuscriptLoginDTO { get; set; }

        public ManuscriptLoginUnitOfWork(string conString)
        {
            _manuscriptLoginRepository = new Repos.ManuscriptLoginRepository(conString);
            _manuscriptLoginDetailsRepository = new Repos.ManuscriptLoginDetailsRepository(_manuscriptLoginRepository.context);
        }

        public void SaveManuscriptLoginDetails()
        {
            foreach (var item in manuscriptLoginDTO.manuscriptLoginDetails)
            {
                if (item.Id == 0 || item.Id == null)
                      _manuscriptLoginDetailsRepository.AddManuscriptLoginDetails(item);
                else
                {
                    _manuscriptLoginDetailsRepository.UpdateManuscriptLoginDetails(item);
                }
                
            }
            _manuscriptLoginDetailsRepository.SaveChanges();
        }
        
        public void SaveManuscriptLogin()
        {
            if (manuscriptLoginDTO.manuscriptLogin.CrestId == 0) // first sent starting from create new then
            {
                manuscriptLoginDTO.manuscriptLogin.CreatedDate = System.DateTime.Now;
                _manuscriptLoginRepository.AddManuscriptLogin(manuscriptLoginDTO.manuscriptLogin);
            }
            else
            {
                manuscriptLoginDTO.manuscriptLogin.ModifiedDate = System.DateTime.Now;
                _manuscriptLoginRepository.UpdateManuscriptLogin(manuscriptLoginDTO.manuscriptLogin);
            }
            _manuscriptLoginRepository.SaveChanges();
        }

        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    //todo:check null to check if instance is created
                    _manuscriptLoginRepository.Dispose();
                    _manuscriptLoginDetailsRepository.Dispose();

                }
            }
            this.disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


    }
}
