using CUAHANGGIAY.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CUAHANGGIAY.Controllers
{
    public class SanPhamController : Controller
    {
        // GET: SanPham
        DataDataContext db;

        public SanPhamController()
        {
            string serverName = Environment.MachineName;
            string defaultDbName = "QLSHOPGIAY";

            // Xây dựng connection string
            string connectionString = $"Data Source={serverName}\\SQLEXPRESS;Initial Catalog={defaultDbName};Integrated Security=True;TrustServerCertificate=True";

            // Khởi tạo DataContext với connection string
            db = new DataDataContext(connectionString);
        }


        // GET: DanhMucList
        public ActionResult Index()
        {
            if (Session["TaiKhoanAdmin"] == null)
            {
                return RedirectToAction("Login", "TK_KH");
            }

            var danhSachSP = db.SANPHAMs
                .Where(sp => sp.IsDeleted == false)
                .Select(sp => new
                {
                    sp.MaSP,
                    sp.TenSP,
                    sp.GiaSP,
                    sp.AnhMinhHoa,
                    TongSoLuong = sp.CHITIETSIZEs.Sum(ct => (int?)ct.SoLuong) ?? 0
                })
                .ToList();

            ViewBag.SanPhamList = danhSachSP;
            return View();
        }

      

        [HttpPost]
        // Thêm sản phẩm vào giỏ hàng
        public JsonResult AddToCart(string maSP, int maSize, int SoLuong)
        {
            string maKH = Session["MaKH"] as string;
            if (string.IsNullOrEmpty(maKH))
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập." });
            }

            // Kiểm tra sản phẩm tồn tại
            var sanPham = db.SANPHAMs.FirstOrDefault(sp => sp.MaSP == maSP && sp.IsDeleted == false);
            if (sanPham == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại." });
            }

            // Kiểm tra tồn kho
            var tonKho = db.CHITIETSIZEs.FirstOrDefault(ct => ct.MaSP == maSP && ct.MaSize == maSize);
            if (tonKho == null || tonKho.SoLuong <= 0)
            {
                return Json(new { success = false, message = "Size hoặc sản phẩm đã hết hàng." });
            }

            if (SoLuong <= 0)
            {
                return Json(new { success = false, message = "Số lượng không hợp lệ." });
            }

            if (SoLuong > tonKho.SoLuong)
            {
                return Json(new { success = false, message = "Số lượng vượt quá tồn kho." });
            }

            // Kiểm tra giỏ hàng
            var gioHang = db.GIOHANGs.FirstOrDefault(gh => gh.MaKH == maKH && gh.MaSP == maSP && gh.MaSize == maSize);

            if (gioHang != null)
            {
                int tongSoLuong = gioHang.SoLuong + SoLuong;
                if (tongSoLuong <= tonKho.SoLuong)
                {
                    gioHang.SoLuong = tongSoLuong;
                    gioHang.ThanhTien = gioHang.SoLuong * sanPham.GiaSP;
                }
                else
                {
                    return Json(new { success = false, message = "Số lượng cộng dồn vượt quá tồn kho." });
                }
            }
            else
            {
                gioHang = new GIOHANG
                {
                    MaKH = maKH,
                    MaSP = maSP,
                    MaSize = maSize,
                    SoLuong = SoLuong,
                    ThanhTien = sanPham.GiaSP * SoLuong
                };
                db.GIOHANGs.InsertOnSubmit(gioHang);
            }

            try
            {
                db.SubmitChanges();
                return Json(new { success = true, message = "Thêm vào giỏ hàng thành công." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Đã xảy ra lỗi: " + ex.Message });
            }
        }

        public ActionResult Details (string id)
        {
            var sanPham = db.SANPHAMs.FirstOrDefault(sp => sp.MaSP == id);

            if (sanPham == null)
            {
                return HttpNotFound();
            }

            // Nếu không có size nào thì vẫn gán danh sách rỗng
            var sizeList = db.CHITIETSIZEs.Where(x => x.MaSP == id).ToList();
            ViewBag.SizeList = sizeList ?? new List<CHITIETSIZE>();

            return View("Details", sanPham);
        }


        public JsonResult GetListSanPham(int pageNumber, int pageSize)
        {
            var listSanPham = db.SANPHAMs
                .Where(sp => sp.IsDeleted == false)
                .Join(db.DANHMUCs,
                      sp => sp.MaDM,
                      dm => dm.MaDM,
                      (sp, dm) => new
                      {
                          sp.MaSP,
                          sp.TenSP,
                          dm.TenDM,
                          sp.MauSP,
                          sp.GiaSP,
                          sp.AnhMinhHoa,
                          sp.ThuongHieu,
                          sp.ChatLieu,
                          sp.KieuDang,
                         
                      })
                .OrderBy(sp => sp.MaSP)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            int tongSoSanPham = db.SANPHAMs.Count(sp => sp.IsDeleted == false);

            return Json(new { listSanPham, tongSoSanPham }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult SearchProductsByName(string name, int pageNumber, int pageSize)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Json(new { listSanPham = new List<object>(), tongSoSanPham = 0 }, JsonRequestBehavior.AllowGet);

            var productList = db.SANPHAMs
                .Where(sp => sp.TenSP.ToLower().Contains(name.ToLower()) && sp.IsDeleted == false)
                .Join(db.DANHMUCs,
                      sp => sp.MaDM,
                      dm => dm.MaDM,
                      (sp, dm) => new
                      {
                          sp.MaSP,
                          sp.TenSP,
                          dm.TenDM,
                          sp.MauSP,
                          sp.GiaSP,
                          sp.AnhMinhHoa,
                          sp.ThuongHieu,
                          sp.ChatLieu,
                          sp.KieuDang,
                      
                      })
                .OrderBy(sp => sp.TenSP)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            int tongSoSanPham = db.SANPHAMs.Count(sp => sp.TenSP.ToLower().Contains(name.ToLower()) && sp.IsDeleted == false);

            return Json(new { listSanPham = productList, tongSoSanPham }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetProductDetail(string id)
        {
            try
            {
                var product = db.SANPHAMs
                    .Where(sp => sp.MaSP == id && sp.IsDeleted == false)
                    .Join(db.DANHMUCs,
                          sp => sp.MaDM,
                          dm => dm.MaDM,
                          (sp, dm) => new
                          {
                              sp.MaSP,
                              sp.TenSP,
                              dm.TenDM,
                              sp.MauSP,
                              sp.GiaSP,
                              sp.ThuongHieu,
                              sp.ChatLieu,
                              sp.KieuDang,
                           
                              sp.Mota,
                              sp.AnhMinhHoa,
                              Sizes = db.CHITIETSIZEs
                                          .Where(s => s.MaSP == sp.MaSP)
                                          .Join(db.SIZEs,
                                                cs => cs.MaSize,
                                                s => s.MaSize,
                                                (cs, s) => new { s.MaSize, s.TenSize, cs.SoLuong })
                                          .ToList()
                          })
                    .FirstOrDefault();

                if (product == null)
                    return Json(new { success = false, message = "Sản phẩm không tồn tại hoặc đã bị xóa." }, JsonRequestBehavior.AllowGet);

                return Json(new { success = true, product }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}


    