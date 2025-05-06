using DATN.Models;
using DATN.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace DATN.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class DashboardController : BaseController
    {
        private readonly DATNDbContext _context;

        public DashboardController(DATNDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            var sessionData = HttpContext.Session.GetString("AdminLogin");
            if (string.IsNullOrEmpty(sessionData))
            {
                return RedirectToAction("Index", "Login");
            }
            var user_admin = JsonConvert.DeserializeObject<UserStaff>(HttpContext.Session.GetString("AdminLogin"));

            var studentCount = await _context.Students.CountAsync();

            var dateLearnCountInMonth = await _context.DateLearns.Where(x => x.Timeline.Value.Month == DateTime.Now.Month).CountAsync();

            var totalAttendance = await (from datelearn in _context.DateLearns
                              join detailattendance in _context.DetailAttendances on datelearn.Id equals detailattendance.DateLearn
                              join attendance in _context.Attendances on detailattendance.IdAttendance equals attendance.Id
                              where datelearn.Timeline.Value.Month == DateTime.Now.Month select datelearn).CountAsync();

            var attendanceCount = await (from datelearn in _context.DateLearns
                                join detailattendance in _context.DetailAttendances on datelearn.Id equals detailattendance.DateLearn
                                join attendance in _context.Attendances on detailattendance.IdAttendance equals attendance.Id
                                where datelearn.Timeline.Value.Month == DateTime.Now.Month && (detailattendance.BeginClass == 1 || detailattendance.EndClass == 1 || detailattendance.BeginClass == 4 || detailattendance.EndClass == 4)
                                         select datelearn).CountAsync();

            var attendanceRate = (attendanceCount * 100.0) / totalAttendance;

            var totalTerm = await (from datelearn in _context.DateLearns
                                join detailterm in _context.DetailTerms on datelearn.DetailTerm equals detailterm.Id
                                join pointprocess in _context.PointProcesses on detailterm.Id equals pointprocess.DetailTerm
                                where datelearn.Timeline.Value.Month == DateTime.Now.Month
                                select datelearn).CountAsync();

            var passCount = await (from datelearn in _context.DateLearns
                                join detailterm in _context.DetailTerms on datelearn.DetailTerm equals detailterm.Id
                                join pointprocess in _context.PointProcesses on detailterm.Id equals pointprocess.DetailTerm
                                where datelearn.Timeline.Value.Month == DateTime.Now.Month && pointprocess.OverallScore < 4
                                select datelearn).CountAsync();

            var passRate = (passCount * 100.0) / totalTerm;

            return View();
        }
    }
}
