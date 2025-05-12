using DATN.Models;
using DATN.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NuGet.ContentModel;

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

            var attendanceRate = Math.Round((attendanceCount * 100.0) / totalAttendance, 0);

            var totalTerm = await (from datelearn in _context.DateLearns
                                join detailterm in _context.DetailTerms on datelearn.DetailTerm equals detailterm.Id
                                join pointprocess in _context.PointProcesses on detailterm.Id equals pointprocess.DetailTerm
                                where datelearn.Timeline.Value.Month == DateTime.Now.Month
                                select datelearn).CountAsync();

            var failCount = await (from datelearn in _context.DateLearns
                                join detailterm in _context.DetailTerms on datelearn.DetailTerm equals detailterm.Id
                                join pointprocess in _context.PointProcesses on detailterm.Id equals pointprocess.DetailTerm
                                where datelearn.Timeline.Value.Month == DateTime.Now.Month && pointprocess.OverallScore < 4
                                select datelearn).CountAsync();

             var failRate = Math.Round((failCount * 100.0) / totalTerm, 0);

            var data = await (from detailTerms in _context.DetailTerms
                              join dateLearns in _context.DateLearns on detailTerms.Id equals dateLearns.DetailTerm
                              join detailAttendances in _context.DetailAttendances on dateLearns.Id equals detailAttendances.DateLearn
                              join attendances in _context.Attendances on detailAttendances.IdAttendance equals attendances.Id
                              where dateLearns.Timeline.HasValue &&
                                    dateLearns.Timeline.Value.Month == DateTime.Now.Month
                              group new { dateLearns, detailAttendances } by detailTerms.TermClass into g
                              select new
                              {
                                  name = g.Key,
                                  totalClasses = g.Select(x => x.dateLearns.Id).Distinct().Count(),
                                  attendance = Math.Round(
                                      (g.Count(x =>
                                          x.detailAttendances.BeginClass == 1 ||
                                          x.detailAttendances.EndClass == 1 ||
                                          x.detailAttendances.BeginClass == 4 ||
                                          x.detailAttendances.EndClass == 4
                                      ) * 100.0) / g.Count(), 0),
                                  absent = g.Count() - g.Count(x =>
                                          x.detailAttendances.BeginClass == 1 ||
                                          x.detailAttendances.EndClass == 1 ||
                                          x.detailAttendances.BeginClass == 4 ||
                                          x.detailAttendances.EndClass == 4),
                              }).ToListAsync();

            ViewBag.studentCount = studentCount;
            ViewBag.dateLearnCountInMonth = dateLearnCountInMonth;
            ViewBag.attendanceRate = attendanceRate;
            ViewBag.failRate = failRate;
            ViewBag.ClassesData = JsonConvert.SerializeObject(data);

            return View();
        }
    }
}
