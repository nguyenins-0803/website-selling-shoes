using CUAHANGGIAY.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CUAHANGGIAY.Controllers
{
    public class HomeController : Controller
    {
        DataDataContext db;

        public HomeController()
        {
            // Lấy tên máy chủ hiện tại
            string serverName = Environment.MachineName;
            string defaultDbName = "QLSHOPGIAY";

            // Xây dựng connection string
            string connectionString = $"Data Source=YENNGTH-0803\\MSSQLSERVER01;Initial Catalog=QLSHOPGIAY;Integrated Security=True";

            // Khởi tạo DataContext với connection string
            db = new DataDataContext(connectionString);
        }
        public ActionResult Index(string searchTerm, string childCategory)
        {
            var products = db.SANPHAMs
                             .Where(p => p.IsDeleted == false) // Không còn kiểm tra SoLuong ở đây nữa
                             .AsQueryable();

            // Lọc sản phẩm theo tên tìm kiếm
            if (!string.IsNullOrEmpty(searchTerm))
            {
                products = products.Where(p => p.TenSP.Contains(searchTerm));
            }

            // Lọc sản phẩm theo danh mục con
            if (!string.IsNullOrEmpty(childCategory))
            {
                var categoryId = db.DANHMUCs
                                   .Where(dm => dm.TenDM == childCategory)
                                   .Select(dm => dm.MaDM)
                                   .FirstOrDefault();

                products = products.Where(p => p.MaDM == categoryId);
            }

            // Chỉ lấy những sản phẩm có ít nhất 1 size còn hàng trong bảng CHITIETSIZE
            var availableProductIds = db.CHITIETSIZEs
                                        .Where(c => c.SoLuong > 0)
                                        .Select(c => c.MaSP)
                                        .Distinct()
                                        .ToList();

            products = products.Where(p => availableProductIds.Contains(p.MaSP));

            // Truyền danh mục cha và con về view
            ViewBag.ParentCategories = db.DANHMUCs
                .Where(dm => dm.DanhMucCha == null)
                .Select(dm => dm.TenDM)
                .ToList();

            ViewBag.ChildCategories = db.DANHMUCs
                .Where(dm => dm.DanhMucCha != null)
                .ToList();

            ViewBag.SelectedParentCategory = db.DANHMUCs
                .Where(dm => dm.MaDM == childCategory)
                .Select(dm => dm.DanhMucCha)
                .FirstOrDefault();

            ViewBag.SelectedChildCategory = childCategory;

            return View(products.ToList());
        }
        public JsonResult GetDanhMucCon(string danhMucCha)
        {
            var danhMucCon = db.DANHMUCs
                .Where(dm => dm.DanhMucCha == danhMucCha && dm.IsDeleted == false) // Thêm điều kiện IsDeleted = false
                .Select(dm => new { dm.MaDM, dm.TenDM })
                .ToList();

            return Json(new { data = danhMucCon }, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetProductsByCategory(string categoryId)
        {
            // Lấy các sản phẩm có ít nhất 1 size còn hàng
            var availableProductIds = db.CHITIETSIZEs
                .Where(ct => ct.SoLuong > 0)
                .Select(ct => ct.MaSP)
                .Distinct()
                .ToList();

            // Lọc sản phẩm theo danh mục, chưa bị xoá và còn hàng
            var products = db.SANPHAMs
                .Where(p => p.MaDM == categoryId && p.IsDeleted == false && availableProductIds.Contains(p.MaSP))
                .Select(p => new
                {
                    p.MaSP,
                    p.TenSP,
                    p.GiaSP,
                    p.AnhMinhHoa
                })
                .ToList();

            return Json(new { data = products }, JsonRequestBehavior.AllowGet);
        }
        // Action để thêm sản phẩm vào giỏ hàng

        [HttpPost]
        //public JsonResult AddToCart(string id) // Sửa MaSP thành id (cần điều chỉnh kiểu dữ liệu)
        //{
        //    var product = db.SANPHAMs.SingleOrDefault(p => p.MaSP == id); // Sử dụng SingleOrDefault để tìm sản phẩm
        //    if (product != null)
        //    {
        //        // Logic để thêm sản phẩm vào giỏ hàng (cần thêm logic cụ thể nếu cần)
        //        return Json(new { success = true, message = "Sản phẩm đã được thêm vào giỏ hàng." });
        //    }
        //    return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại." });
        //}
        // Action để tìm kiếm sản phẩm
        [HttpGet]
        public ActionResult SearchProducts(string searchTerm, string childCategory)
        {
            // Lấy danh sách sản phẩm còn hàng từ bảng CHITIETSIZE
            var availableProductIds = db.CHITIETSIZEs
                .Where(ct => ct.SoLuong > 0)
                .Select(ct => ct.MaSP)
                .Distinct()
                .ToList();

            var products = db.SANPHAMs
                .Where(p => p.IsDeleted == false && availableProductIds.Contains(p.MaSP))
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                products = products.Where(p => p.TenSP.Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(childCategory))
            {
                var categoryId = db.DANHMUCs
                    .Where(dm => dm.TenDM == childCategory)
                    .Select(dm => dm.MaDM)
                    .FirstOrDefault();

                products = products.Where(p => p.MaDM == categoryId);
            }

            return View("Index", products.ToList());
        }

        public ActionResult DangXuat()
        {
            // Xóa session và chuyển hướng về trang đăng nhập
            Session["TenKH"] = null;
            return RedirectToAction("Index", "Home");
        }
        // Action xử lý điều hướng khi bấm vào "LAPTOP GO"


    }
}