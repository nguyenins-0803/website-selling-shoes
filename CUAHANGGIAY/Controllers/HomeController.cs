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
            string serverName = Environment.MachineName;
            string defaultDbName = "QLSHOPGIAY";

            // Xây dựng connection string
            string connectionString = $"Data Source={serverName}\\SQLEXPRESS;Initial Catalog={defaultDbName};Integrated Security=True;TrustServerCertificate=True";

            // Khởi tạo DataContext với connection string
            db = new DataDataContext(connectionString);
        }
        private void LoadFilterData()
        {
            ViewBag.SizeList = db.SIZEs.Select(s => s.TenSize).Distinct().OrderBy(s => s).ToList();

            if (ViewBag.SizeList == null)
            {
                ViewBag.SizeList = new List<string>(); // Tránh null reference exception
            }

            // Tiếp tục với các filter khác
            ViewBag.MauSPList = db.SANPHAMs.Where(s => s.IsDeleted == false)
                                           .Select(s => s.MauSP)
                                           .Distinct()
                                           .ToList();
            ViewBag.KieuDangList = db.SANPHAMs.Where(s => s.IsDeleted == false)
                                              .Select(s => s.KieuDang)
                                              .Distinct()
                                              .ToList();
            ViewBag.ChatLieuList = db.SANPHAMs.Where(s => s.IsDeleted == false)
                                              .Select(s => s.ChatLieu)
                                              .Distinct()
                                              .ToList();
            ViewBag.ThuongHieuList = db.SANPHAMs.Where(s => s.IsDeleted == false)
                                                .Select(s => s.ThuongHieu)
                                                .Distinct()
                                                .ToList();

        }



        public ActionResult Index(string searchTerm)
        {
            var products = db.SANPHAMs
                             .Where(p => p.IsDeleted == false)
                             .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                products = products.Where(p => p.TenSP.Contains(searchTerm));
            }


            var availableProductIds = db.CHITIETSIZEs
                                        .Where(c => c.SoLuong > 0)
                                        .Select(c => c.MaSP)
                                        .Distinct()
                                        .ToList();

            products = products.Where(p => availableProductIds.Contains(p.MaSP));

            //ViewBag.SizeList = db.SIZEs.Select(s => s.TenSize).Distinct().OrderBy(s => s).ToList();
            //var mauSPList = db.SANPHAMs.Select(p => p.MauSP).Distinct().ToList();
            //ViewBag.MauSPList = mauSPList.Any() ? mauSPList : new List<string>();

            //// >>> Thêm 3 ViewBag bị thiếu vào đây:
            //ViewBag.ThuongHieuList = db.SANPHAMs.Where(s => s.IsDeleted == false)
            //                        .Select(s => s.ThuongHieu)
            //                        .Distinct()
            //                        .ToList();

            //ViewBag.KieuDangList = db.SANPHAMs.Where(s => s.IsDeleted == false)
            //                        .Select(s => s.KieuDang)
            //                        .Distinct()
            //                        .ToList();

            //ViewBag.ChatLieuList = db.SANPHAMs.Where(s => s.IsDeleted == false)
            //                        .Select(s => s.ChatLieu)
            //                        .Distinct()
            //                        .ToList();

            LoadSidebarData();
            return View(products.ToList());
        }

        //public JsonResult GetDanhMucCon(string danhMucCha)
        //{
        //    if (string.IsNullOrEmpty(danhMucCha))
        //    {
        //        return Json(new { data = new List<string>() }, JsonRequestBehavior.AllowGet);
        //    }

        //    var danhMucCon = db.DANHMUCs
        //        .Where(dm => dm.DanhMucCha == danhMucCha && dm.IsDeleted == false)
        //        .Select(dm => new { dm.MaDM, dm.TenDM })
        //        .ToList();

        //    return Json(new { data = danhMucCon }, JsonRequestBehavior.AllowGet);
        //}

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

        public ActionResult Filter(string sizeFilter, string mauSP, string kieuDang, string chatLieu, string thuongHieu, string sortBy)
        {

            LoadFilterData();
            // Load danh sách sản phẩm trước
            var products = db.SANPHAMs.Where(s => s.IsDeleted == false);

            // Apply filter nếu có
            if (!string.IsNullOrEmpty(sizeFilter))
            {
                var maSPList = db.CHITIETSIZEs.Where(ct => ct.MaSize.ToString() == sizeFilter)
                                              .Select(ct => ct.MaSP)
                                              .Distinct()
                                              .ToList();
                products = products.Where(p => maSPList.Contains(p.MaSP));
            }

            if (!string.IsNullOrEmpty(mauSP))
            {
                products = products.Where(p => p.MauSP == mauSP);
            }

            if (!string.IsNullOrEmpty(kieuDang))
            {
                products = products.Where(p => p.KieuDang == kieuDang);
            }

            if (!string.IsNullOrEmpty(chatLieu))
            {
                products = products.Where(p => p.ChatLieu == chatLieu);
            }

            if (!string.IsNullOrEmpty(thuongHieu))
            {
                products = products.Where(p => p.ThuongHieu == thuongHieu);
            }

            // Sort
            switch (sortBy)
            {
                case "PriceAsc":
                    products = products.OrderBy(p => p.GiaSP);
                    break;
                case "PriceDesc":
                    products = products.OrderByDescending(p => p.GiaSP);
                    break;
                case "NameAsc":
                    products = products.OrderBy(p => p.TenSP);
                    break;
                case "NameDesc":
                    products = products.OrderByDescending(p => p.TenSP);
                    break;
            }
         

            var productList = products.ToList();

            // Load filter option list vào ViewBag (giống như LoadFilterData)
            ViewBag.SizeList = db.SIZEs.Select(s => s.TenSize).Distinct().OrderBy(s => s).ToList();
            ViewBag.MauSPList = db.SANPHAMs.Where(s => s.IsDeleted == false).Select(s => s.MauSP).Distinct().ToList();
            ViewBag.KieuDangList = db.SANPHAMs.Where(s => s.IsDeleted == false).Select(s => s.KieuDang).Distinct().ToList();
            ViewBag.ChatLieuList = db.SANPHAMs.Where(s => s.IsDeleted == false).Select(s => s.ChatLieu).Distinct().ToList();
            ViewBag.ThuongHieuList = db.SANPHAMs.Where(s => s.IsDeleted == false).Select(s => s.ThuongHieu).Distinct().ToList();

            // Truyền lại các giá trị filter để set selected option
            ViewBag.SelectedSize = sizeFilter;
            ViewBag.SelectedColor = mauSP;
            ViewBag.SelectedStyle = kieuDang;
            ViewBag.SelectedMaterial = chatLieu;
            ViewBag.SelectedBrand = thuongHieu;
            ViewBag.SelectedSort = sortBy;

            return View("Index", productList);
        }

        public ActionResult SearchProducts(string searchTerm)
        {
            LoadFilterData();
            var products = db.SANPHAMs.Where(s => s.IsDeleted == false);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                products = products.Where(p =>
                    p.TenSP.Contains(searchTerm) ||
                    p.ThuongHieu.Contains(searchTerm) ||
                    p.KieuDang.Contains(searchTerm) ||
                    p.ChatLieu.Contains(searchTerm)
                );
            }

            var productList = products.ToList();

            // Load danh sách thương hiệu cho filter/menu nếu có
            ViewBag.ThuongHieuList = db.SANPHAMs.Where(s => s.IsDeleted == false)
                                                .Select(s => s.ThuongHieu)
                                                .Distinct()
                                                .ToList();

            // Truyền lại giá trị search để giữ nguyên input
            ViewBag.SearchTerm = searchTerm;

            return View("Index", productList);
        }

        //public ActionResult DanhMuc()
        //{
        //    // Lấy danh sách distinct các ThuongHieu
        //    var thuongHieuList = db.SANPHAMs.Where(s => s.IsDeleted == false)
        //                        .Select(s => s.ThuongHieu)
        //                        .Distinct()
        //                        .ToList();

        //    // Tương tự cho KieuDang và ChatLieu
        //    var kieuDangList = db.SANPHAMs.Where(s => s.IsDeleted == false)
        //                        .Select(s => s.KieuDang)
        //                        .Distinct()
        //                        .ToList();

        //    var chatLieuList = db.SANPHAMs.Where(s => s.IsDeleted == false)
        //                        .Select(s => s.ChatLieu)
        //                        .Distinct()
        //                        .ToList();

        //    // Truyền qua ViewBag
        //    ViewBag.ThuongHieuList = thuongHieuList;
        //    ViewBag.KieuDangList = kieuDangList;
        //    ViewBag.ChatLieuList = chatLieuList;

        //    return View();
        //}
        private void LoadSidebarData()
        {
            ViewBag.ThuongHieuList = db.SANPHAMs.Where(s => s.IsDeleted == false)
                                    .Select(s => s.ThuongHieu)
                                    .Distinct()
                                    .ToList();

            ViewBag.KieuDangList = db.SANPHAMs.Where(s => s.IsDeleted == false)
                                    .Select(s => s.KieuDang)
                                    .Distinct()
                                    .ToList();

            ViewBag.ChatLieuList = db.SANPHAMs.Where(s => s.IsDeleted == false)
                                    .Select(s => s.ChatLieu)
                                    .Distinct()
                                    .ToList();

            ViewBag.MauSPList = db.SANPHAMs.Where(s => s.IsDeleted == false)
                                  .Select(s => s.MauSP)
                                  .Distinct()
                                  .ToList();

            ViewBag.SizeList = db.SIZEs.Select(s => s.TenSize)
                                  .Distinct()
                                  .OrderBy(s => s)
                                  .ToList();
        }

        public ActionResult SanPhamTheoThuongHieu(string thuongHieu)
        {
            var sp = db.SANPHAMs.Where(s => s.ThuongHieu == thuongHieu && s.IsDeleted == false).ToList();
            LoadSidebarData();
            return View("Index", sp);
        }
        public ActionResult SanPhamTheoKieuDang(string kieuDang)
        {
            var sp = db.SANPHAMs.Where(s => s.KieuDang == kieuDang && s.IsDeleted == false).ToList();
            LoadSidebarData();
            return View("Index", sp);
        }

        public ActionResult SanPhamTheoChatLieu(string chatLieu)
        {
            var sp = db.SANPHAMs.Where(s => s.ChatLieu == chatLieu && s.IsDeleted == false).ToList();
            LoadSidebarData();
            return View("Index", sp);
        }
        public ActionResult SanPhamTheoGia(int min, int max)
        {
            var sp = db.SANPHAMs.Where(s => s.GiaSP >= min && s.GiaSP <= max && s.IsDeleted == false).ToList();
            LoadSidebarData();
            return View("Index", sp);
        }

        // Action để thêm sản phẩm vào giỏ hàng

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
     
        
        public ActionResult DangXuat()
        {
            // Xóa session và chuyển hướng về trang đăng nhập
            Session["TenKH"] = null;
            return RedirectToAction("Index", "Home");
        }
        // Action xử lý điều hướng khi bấm vào "SHOP ALY"


    }
}