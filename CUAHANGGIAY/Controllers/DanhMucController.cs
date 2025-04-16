using CUAHANGGIAY.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CUAHANGGIAY.Controllers
{
    public class DanhMucController : Controller
    {
        // GET: DanhMuc
        DataDataContext _danhMucContext;

        public DanhMucController()
        {
            // Lấy tên máy chủ hiện tại
            string serverName = Environment.MachineName;
            string defaultDbName = "QLSHOPGIAY";

            // Xây dựng connection string
            string connectionString = $"Data Source=YENNGTH-0803\\MSSQLSERVER01;Initial Catalog=QLSHOPGIAY;Integrated Security=True";

            // Khởi tạo DataContext với connection string
            _danhMucContext = new DataDataContext(connectionString);
        }


        // GET: DanhMucList
        public ActionResult DanhMuc()
        {
            if (Session["TaiKhoanAdmin"] == null)
            {
                return RedirectToAction("Login", "TK_KH");
            }
            return View();
        }

        // GET: Create Danh Muc
        public ActionResult Create()
        {
            if (Session["TaiKhoanAdmin"] == null)
            {
                return RedirectToAction("Login", "TK_KH");
            }
            return View();
        }

        //API for pagination to get list of ProductCatalogues

        public JsonResult GetListDanhMuc(int pageNumber, int pageSize)
        {
            var listDanhMuc = _danhMucContext.DANHMUCs
                .Where(x => x.IsDeleted == false)  // Lọc các danh mục chưa bị xóa
                .Select(x => new
                {
                    x.MaDM,
                    x.TenDM,
                    x.DanhMucCha
                })
                .OrderBy(sp => sp.MaDM)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            if (listDanhMuc == null || listDanhMuc.Count == 0)
            {
                return Json(new { success = false, message = "Không có danh mục." }, JsonRequestBehavior.AllowGet);
            }

            var tongSoDanhMuc = _danhMucContext.DANHMUCs.Count(x => x.IsDeleted == false);  // Lọc theo danh mục chưa bị xóa
            return Json(new { listDanhMuc, tongSoDanhMuc }, JsonRequestBehavior.AllowGet);
        }




        [HttpPost]
        public ActionResult GetListDanhMucForSP()
        {
            // Lấy danh sách danh mục chưa bị xóa (IsDeleted = false)
            List<DANHMUC> danhMucList = _danhMucContext.DANHMUCs
                .Where(d => d.IsDeleted == false)  // Lọc chỉ những danh mục chưa bị xóa
                .ToList();

            // Kiểm tra nếu danh sách rỗng hoặc null
            if (danhMucList == null || danhMucList.Count == 0)
            {
                return Json(new { data = new List<string>() }, JsonRequestBehavior.AllowGet); // Trả về danh sách rỗng
            }

            // Trả về dữ liệu dưới dạng JSON
            var result = danhMucList.Select(d => new
            {
                MaDM = d.MaDM,
                TenDM = d.TenDM,
                DanhMucCha = d.DanhMucCha,
            }).ToList();

            return Json(new { data = result }, JsonRequestBehavior.AllowGet);
        }


        public JsonResult GetObjectByID(string id)
        {
            Debug.WriteLine(id);

            // Lấy danh mục theo MaDM và kiểm tra nếu danh mục chưa bị xóa (IsDeleted = false)
            var catalogueObj = _danhMucContext.DANHMUCs
                .FirstOrDefault(x => x.MaDM == id && x.IsDeleted == false);

            if (catalogueObj == null)
            {
                return Json(new { success = false, message = "Danh mục không tồn tại hoặc đã bị xóa." }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = true, MaDM = catalogueObj.MaDM, TenDM = catalogueObj.TenDM, DanhMucCha = catalogueObj.DanhMucCha }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]

        public JsonResult SuaDanhMuc(string id, string ten, string danhMucCha)
        {
            // Kiểm tra xem danh mục cha có hợp lệ hay không
            var validDanhMucChas = new[] { "Giá", "Hãng", "Nhu cầu" };
            if (!validDanhMucChas.Contains(danhMucCha))
            {
                return Json(new { success = false, message = "Danh mục cha không hợp lệ." }, JsonRequestBehavior.AllowGet);
            }

            var catalogueObj = _danhMucContext.DANHMUCs.FirstOrDefault(x => x.MaDM == id);
            if (catalogueObj == null)
            {
                return Json(new { success = false, message = "Danh mục không tồn tại." }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                catalogueObj.TenDM = ten;
                catalogueObj.DanhMucCha = danhMucCha;
                _danhMucContext.SubmitChanges();
                return Json(new { success = true, message = "Danh mục sửa thành công!" }, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpPost]

        public JsonResult XoaDanhMuc(string id)
        {
            var catalogueObj = _danhMucContext.DANHMUCs.FirstOrDefault(x => x.MaDM == id);
            if (catalogueObj == null)
            {
                return Json(new { success = false, message = "Danh mục không tồn tại." });
            }

            // Thay vì xóa cứng, đánh dấu IsDeleted = true
            catalogueObj.IsDeleted = true;
            _danhMucContext.SubmitChanges();

            return Json(new { success = true, message = "Danh mục đã được xóa thành công." });
        }


        public JsonResult TimKiemDanhMuc(string name)
        {
            var productList = _danhMucContext.DANHMUCs
               .Where(x => x.TenDM.Contains(name) && x.IsDeleted == false)  // Lọc các danh mục chưa bị xóa
               .Select(x => new
               {
                   x.MaDM,
                   x.TenDM,
                   x.DanhMucCha
               })
               .ToList();

            return Json(productList, JsonRequestBehavior.AllowGet);
        }


        [HttpPost]
        public JsonResult TaoDanhMuc(string tenDanhMuc, string DanhMucCha)
        {
            try
            {
                // Kiểm tra xem danh mục cha có hợp lệ hay không
                var validDanhMucChas = new[] { "Giá", "Hãng", "Nhu cầu" };
                DanhMucCha = DanhMucCha?.Trim(); // Loại bỏ khoảng trắng thừa

                // Đảm bảo so sánh không phân biệt chữ hoa chữ thường
                if (!validDanhMucChas.Contains(DanhMucCha, StringComparer.OrdinalIgnoreCase))
                {
                    return Json(new { success = false, message = "Danh mục cha không hợp lệ." }, JsonRequestBehavior.AllowGet);
                }

                // Kiểm tra tên danh mục có bị trùng không
                var existingDanhMuc = _danhMucContext.DANHMUCs
                    .FirstOrDefault(dm => dm.TenDM.ToLower() == tenDanhMuc.ToLower());

                if (existingDanhMuc != null)
                {
                    return Json(new { success = false, message = "Tên danh mục đã tồn tại." }, JsonRequestBehavior.AllowGet);
                }

                // Thêm mới danh mục
                DANHMUC danhMucObj = new DANHMUC
                {
                    TenDM = tenDanhMuc,
                    DanhMucCha = DanhMucCha,
                    IsDeleted = false // Mặc định là chưa bị xóa
                };

                _danhMucContext.DANHMUCs.InsertOnSubmit(danhMucObj);
                _danhMucContext.SubmitChanges();

                return Json(new { success = true, message = "Danh mục đã được thêm thành công." }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { success = false, message = e.Message }, JsonRequestBehavior.AllowGet);
            }
        }

    }
}