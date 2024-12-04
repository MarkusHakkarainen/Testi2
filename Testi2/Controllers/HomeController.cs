using System.Web.Mvc;

namespace Testi2.Controllers
{
    public class HomeController : Controller
    {
        // Etusivu
        public ActionResult Index()
        {
            if (Session["UserEmail"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.UserEmail = Session["UserEmail"];
            return View();
        }

        // Kirjautuminen ulos
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login", "Account");
        }
    }
}
