using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pelican.DataAccess.Repository.IRepository;
using Pelican.Models;
using Pelican.Utility;

namespace MVC_tutorial.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CompanyController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public CompanyController(IUnitOfWork db)
        {
            _unitOfWork = db;
        }
        public IActionResult Index()
        {
            List<Company> Companys = _unitOfWork.Company.GetAll().ToList();
            return View(Companys);
        }

        public IActionResult Upsert(int? id)
        {
            if(id==null || id==0)
            {
                //Create
                return View(new Company());
            }
            else
            {
                //Update
                Company companyObj = _unitOfWork.Company.Get(u=>u.Id==id);
                return View(companyObj);
            }
        }
        [HttpPost]
        public IActionResult Upsert(Company obj, IFormFile? file)
        {
            string state;
            if (ModelState.IsValid)
            {
                if(obj.Id == 0)
                {
                    _unitOfWork.Company.Add(obj);
                    state = "created";
                }
                else
                {
                    _unitOfWork.Company.Update(obj);
                    state = "updated";
                }
                _unitOfWork.Save();
                TempData["success"] = $"Company {state} successfully";
                return RedirectToAction("Index", "Company");
            }
            else
            {
                return View(obj);
            }
        }

        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            List<Company> objCompanyList = _unitOfWork.Company.GetAll().ToList();
            return Json(new {data = objCompanyList});
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var CompanyToBeDeleted = _unitOfWork.Company.Get(u=>u.Id==id);
            if(CompanyToBeDeleted == null) 
            {
                return Json(new {success=false, message="Error while deleting."});
            }
            
            _unitOfWork.Company.Remove(CompanyToBeDeleted);
            _unitOfWork.Save();

            return Json(new { success = true, message = "Deleted Successful." });
        }
        #endregion
    }
}
