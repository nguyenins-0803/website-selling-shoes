using CUAHANGGIAY.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CUAHANGGIAY.Controllers
{
    public class DH_KHController : Controller
    {
       DataDataContext db;

        public DH_KHController()
        {
            // Lấy tên máy chủ hiện tại
            string serverName = Environment.MachineName;
            string defaultDbName = "QLSHOPGIAY";

            // Xây dựng connection string
            string connectionString = $"Data Source={serverName}\\MSSQLSERVER01;Initial Catalog={defaultDbName};Integrated Security=True;TrustServerCertificate=True";

            // Khởi tạo DataContext với connection string
            db = new DataDataContext(connectionString);
        }


        // GET: DonHang/Index (Order History)
        public ActionResult Index()
        {
            // Check if the customer is logged in
            if (Session["MaKH"] == null)
            {
                return RedirectToAction("Login", "DH_KH"); // Redirect to login if no session
            }
            return View();


        }
        public JsonResult GetDonHangs()
        {
            try
            {
                // Kiểm tra xem khách hàng đã đăng nhập chưa
                if (Session["MaKH"] == null)
                {
                    return Json(new { success = false, message = "Bạn cần đăng nhập để xem đơn hàng!" }, JsonRequestBehavior.AllowGet);
                }

                string maKH = Session["MaKH"].ToString();

                var donHangs = db.DONHANGs
                                     .Where(dh => dh.MaKH == maKH) // Lọc theo MaKH từ bảng DONHANG
                                     .Join(db.CHITIETDHs,
                                           dh => dh.MaDH,
                                           ct => ct.MaDH,
                                           (dh, ct) => new { dh, ct })
                                     .Join(db.SANPHAMs,
                                           combined => combined.ct.MaSP,
                                           sp => sp.MaSP,
                                           (combined, sp) => new { combined.dh, combined.ct, sp })  // Kết nối với SANPHAM
                                     .Join(db.SIZEs,
                                           combined => combined.ct.MaSize,
                                           sz => sz.MaSize,
                                           (combined, sz) => new
                                           {
                                               combined.dh.MaDH,
                                               combined.dh.MaKH,
                                               combined.dh.NgayMua,
                                               combined.dh.DiaChi,
                                               combined.dh.ThanhToan,
                                               combined.dh.TongTien,
                                               combined.dh.TrangThai,
                                               TenSP = combined.sp.TenSP,  // Sửa lỗi: dùng combined.sp.TenSP
                                               TenSize = sz.TenSize,
                                               combined.ct.GiaMua,
                                               combined.ct.SLMua
                                           })
                                     .ToList();


                return Json(donHangs, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }



        [HttpGet]
        public JsonResult GetChiTietDonHang(string maDH)
        {
            // Kiểm tra maDH không null hoặc rỗng
            if (string.IsNullOrEmpty(maDH))
            {
                return Json(new { success = false, message = "Mã đơn hàng không hợp lệ" }, JsonRequestBehavior.AllowGet);
            }

            // Truy vấn chi tiết đơn hàng
            // Truy vấn chi tiết đơn hàng
            var orderDetails = (from dh in db.DONHANGs
                                join kh in db.KHACHHANGs on dh.MaKH equals kh.MaKH
                                where dh.MaDH == maDH
                                select new
                                {
                                    DonHang = new
                                    {
                                        dh.MaDH,
                                        kh.TenKH,
                                        kh.SDT,
                                        kh.DiaChi,
                                        dh.NgayMua,
                                        dh.TrangThai,
                                        dh.TongTien
                                    },
                                    // SanPham phải là danh sách (List), thêm MaSize vào
                                    SanPham = (from ctdh in db.CHITIETDHs
                                               join sp in db.SANPHAMs on ctdh.MaSP equals sp.MaSP
                                               join sz in db.SIZEs on ctdh.MaSize equals sz.MaSize // Thêm join với bảng SIZE
                                               where ctdh.MaDH == dh.MaDH
                                               select new
                                               {
                                                   sp.MaSP,
                                                   sp.TenSP,
                                                   ctdh.GiaMua,
                                                   ctdh.SLMua,
                                                   sp.AnhMinhHoa,
                                                   ctdh.MaSize,  // Thêm MaSize
                                                   TenSize = sz.TenSize // Lấy tên size từ bảng SIZE
                                               }).ToList() // ToList để đảm bảo trả về một danh sách
                                }).FirstOrDefault();


            // Kiểm tra nếu không tìm thấy đơn hàng
            if (orderDetails == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng" }, JsonRequestBehavior.AllowGet);
            }

            // Trả về chi tiết đơn hàng
            return Json(new { success = true, orderDetails = orderDetails }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult MarkAsCompleted(string maDH)
        {

            var donHang = db.DONHANGs.FirstOrDefault(d => d.MaDH == maDH);

            if (donHang == null)
            {
                return Json(new { success = false, message = "Đơn hàng không tồn tại!" }, JsonRequestBehavior.AllowGet);
            }

            // Kiểm tra trạng thái của đơn hàng
            if (donHang.TrangThai != "Đang giao")
            {
                return Json(new { success = false, message = "Chỉ có thể xác nhận thành công khi đang giao!" }, JsonRequestBehavior.AllowGet);
            }

            // Thực hiện hủy đơn hàng (thay đổi trạng thái hoặc xóa đơn hàng)
            donHang.TrangThai = "Thành công";
            db.SubmitChanges();

            return Json(new { success = true, message = "Đơn hàng đã được hủy thành công!" }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult HuyDon(string maDH)
        {
            // Tìm đơn hàng theo mã
            var donHang = db.DONHANGs.FirstOrDefault(d => d.MaDH == maDH);
            if (donHang == null)
                return Json(new { success = false, message = "Đơn hàng không tồn tại!" },
                            JsonRequestBehavior.AllowGet);

            // Chỉ cho hủy khi trạng thái = "Đang xử lý"
            if (donHang.TrangThai != "Đang xử lý")
                return Json(new { success = false, message = "Chỉ có thể hủy đơn hàng khi đang xử lý!" },
                            JsonRequestBehavior.AllowGet);

            // Lấy danh sách chi tiết đơn hàng
            var chiTietDonHang = db.CHITIETDHs.Where(ct => ct.MaDH == maDH).ToList();
            if (!chiTietDonHang.Any())
                return Json(new { success = false, message = "Không tìm thấy chi tiết đơn hàng!" },
                            JsonRequestBehavior.AllowGet);

            // Hoàn trả số lượng vào bảng CHITIETSIZE chứ không phải vào SANPHAM
            foreach (var ct in chiTietDonHang)
            {
                var cs = db.CHITIETSIZEs
                           .FirstOrDefault(x => x.MaSP == ct.MaSP && x.MaSize == ct.MaSize);
                if (cs != null)
                {
                    cs.SoLuong += ct.SLMua;
                }
            }

            // Đổi trạng thái đơn hàng
            donHang.TrangThai = "Đã hủy";

            try
            {
                db.SubmitChanges();
                return Json(new
                {
                    success = true,
                    message = "Đơn hàng đã được hủy thành công, số lượng sản phẩm đã được hoàn trả!"
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Lỗi khi xử lý: " + ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }



        // Search Functionality for the logged-in customer
        public ActionResult Search(string query)
        {
            if (Session["MaKH"] == null)
            {
                return RedirectToAction("Login", "DH_KH");
            }

            string maKH = Session["MaKH"].ToString();

            var result = db.DONHANGs
                           .Where(dh => dh.MaKH == maKH && dh.MaDH.Contains(query))
                           .Select(dh => new
                           {
                               dh.MaDH,
                               dh.ThanhToan,
                               dh.TrangThai,
                               dh.NgayMua,
                               dh.DiaChi
                           })
                           .ToList();

            return Json(result, JsonRequestBehavior.AllowGet);
        }
        // Thống kê đơn hàng
        public ActionResult ThongKeDonHang()
        {
            string maKH = Session["MaKH"]?.ToString();
            if (string.IsNullOrEmpty(maKH))
                return RedirectToAction("Login", "DH_KH");

            var donHangs = db.DONHANGs.Where(d => d.MaKH == maKH);

            var thongKe = new
            {
                TongSoDonHang = donHangs.Count(),
                TongSoDonHoanThanh = donHangs.Count(d => d.TrangThai == "Đã giao thành công"),
                TongGiaTri = donHangs.Sum(d => d.TongTien) ?? 0
            };

            ViewBag.ThongKe = thongKe;
            return View();
        }

        [HttpGet]

        public JsonResult GetOrderStats()
        {
            try
            {
                // Lấy mã khách hàng từ session
                string maKH = Session["MaKH"].ToString();

                // Lọc đơn hàng theo MaKH
                var orderStats = db.DONHANGs
                    .Where(o => o.MaKH == maKH) // Lọc đơn hàng theo mã khách hàng
                    .GroupBy(o => o.MaKH) // Nhóm theo mã khách hàng
                    .Select(group => new
                    {
                        MaKH = group.Key, // Mã khách hàng
                        TotalOrders = group.Count(), // Tổng số đơn hàng
                        TotalCompletedOrders = group.Count(o => o.TrangThai == "Thành công"), // Số đơn hàng thành công
                        TotalValueCompleted = group.Where(o => o.TrangThai == "Thành công").Sum(o => o.TongTien), // Tổng giá trị đơn hàng thành công
                        TotalShippingOrders = group.Count(o => o.TrangThai == "Đang giao"), // Số đơn hàng đang giao
                        TotalCancelledOrders = group.Count(o => o.TrangThai == "Đã hủy") // Số đơn hàng đã hủy
                    })
                    .FirstOrDefault(); // Chỉ lấy thống kê của mã khách hàng đang đăng nhập

                // Kiểm tra nếu có kết quả thống kê
                if (orderStats != null)
                {
                    return Json(new
                    {
                        success = true,
                        totalCompletedOrders = orderStats.TotalCompletedOrders,
                        totalValueCompleted = orderStats.TotalValueCompleted,
                        totalShippingOrders = orderStats.TotalShippingOrders,
                        totalCancelledOrders = orderStats.TotalCancelledOrders,
                        totalAllOrders = orderStats.TotalOrders
                    }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { success = false, message = "Không có đơn hàng nào cho khách hàng này!" }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

    }

    // Lớp chứa thông tin thống kê

}
