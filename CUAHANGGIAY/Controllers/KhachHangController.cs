using CUAHANGGIAY.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace CUAHANGGIAY.Controllers
{
    public class KhachHangController : Controller
    {
        // GET: KhachHang
        DataDataContext db;

        public KhachHangController()
        {
            string serverName = Environment.MachineName;
            string defaultDbName = "QLSHOPGIAY";

            // Xây dựng connection string
            string connectionString = $"Data Source={serverName}\\MSSQLSERVER01;Initial Catalog={defaultDbName};Integrated Security=True;TrustServerCertificate=True";

            // Khởi tạo DataContext với connection string
            db = new DataDataContext(connectionString);
        }


        // GET: KhachHang
        public ActionResult Index()
        {
            return View();
        }
        // Hàm hiển thị thông tin khách hàng
        public ActionResult Thong_tin_KH()
        {
            // Lấy MaKH từ session (hoặc có thể lấy từ cookie)
            var maKH = Session["MaKH"] as string;

            if (string.IsNullOrEmpty(maKH))
            {
                return RedirectToAction("Index", "Home"); // Chuyển về trang chủ nếu chưa đăng nhập
            }

            // Tìm khách hàng trong cơ sở dữ liệu theo MaKH
            var khachHang = db.KHACHHANGs.FirstOrDefault(kh => kh.MaKH == maKH);

            if (khachHang == null)
            {
                return HttpNotFound();
            }

            // Trả về view với thông tin khách hàng
            return View(khachHang);
        }


        // Hàm cập nhật thông tin khách hàng

        [HttpPost]

        public ActionResult UpdateInfo(KHACHHANG updatedKhachHang)
        {
            var maKH = Session["MaKH"] as string;

            // Kiểm tra nếu mã khách hàng không hợp lệ (null hoặc rỗng)
            if (string.IsNullOrEmpty(maKH))
            {
                return RedirectToAction("Index", "Home");
            }

            // Tìm khách hàng trong cơ sở dữ liệu theo MaKH
            var khachHang = db.KHACHHANGs.FirstOrDefault(kh => kh.MaKH == maKH);

            // Kiểm tra nếu không tìm thấy khách hàng
            if (khachHang == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Lấy số điện thoại và email từ model
            string sdt = updatedKhachHang.SDT;
            string gmail = updatedKhachHang.Gmail;

            // Kiểm tra nếu số điện thoại hoặc email là null hoặc rỗng
            if (string.IsNullOrEmpty(sdt))
            {
                ViewBag.ErrorMessage = "Số điện thoại không được để trống.";
                return View("Thong_tin_KH", khachHang);  // Trả về view với thông báo lỗi
            }
            if (string.IsNullOrEmpty(gmail))
            {
                ViewBag.ErrorMessage = "Email không được để trống.";
                return View("Thong_tin_KH", khachHang);  // Trả về view với thông báo lỗi
            }

            // Kiểm tra định dạng số điện thoại
            var phoneRegex = new Regex(@"^0\d{9}$");
            if (!phoneRegex.IsMatch(sdt))
            {
                ViewBag.ErrorMessage = "Số điện thoại không hợp lệ. Vui lòng nhập số có 10 chữ số và bắt đầu bằng số 0.";
                return View("Thong_tin_KH", khachHang);  // Trả về view với thông báo lỗi
            }

            // Kiểm tra định dạng email
            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            if (!emailRegex.IsMatch(gmail))
            {
                ViewBag.ErrorMessage = "Email không hợp lệ. Vui lòng nhập email đúng định dạng.";
                return View("Thong_tin_KH", khachHang);  // Trả về view với thông báo lỗi
            }

            // Kiểm tra sự trùng lặp số điện thoại (không phải của chính khách hàng hiện tại)
            if (db.KHACHHANGs.Any(k => k.SDT == sdt && k.MaKH != maKH))
            {
                ViewBag.ErrorMessage = "Số điện thoại đã được sử dụng.";
                return View("Thong_tin_KH", khachHang);  // Trả về view thông tin khách hàng với thông báo lỗi
            }


            // Cập nhật thông tin khách hàng
            khachHang.TenKH = updatedKhachHang.TenKH;
            khachHang.Gmail = updatedKhachHang.Gmail;
            khachHang.DiaChi = updatedKhachHang.DiaChi;
            khachHang.SDT = updatedKhachHang.SDT;

            // Cập nhật vào cơ sở dữ liệu
            try
            {
                db.SubmitChanges();  // Lưu thay đổi vào cơ sở dữ liệu
                ViewBag.SuccessMessage = "Cập nhật thông tin thành công!";
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Có lỗi xảy ra khi cập nhật thông tin. Vui lòng thử lại sau.";
                return View("Thong_tin_KH", khachHang);  // Trả về view với thông báo lỗi
            }

            // Sau khi cập nhật, hiển thị lại trang thông tin khách hàng
            return View("Thong_tin_KH", khachHang);  // Trả về view với thông báo thành công
        }




        public ActionResult Doi_mk()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Doi_mk(string passold, string newpass, string confpass)
        {
            var maKH = Session["MaKH"] as string;

            // Kiểm tra nếu khách hàng chưa đăng nhập
            if (string.IsNullOrEmpty(maKH))
            {
                return RedirectToAction("Index", "Home");
            }

            // Tìm khách hàng trong cơ sở dữ liệu
            var khachHang = db.KHACHHANGs.FirstOrDefault(kh => kh.MaKH == maKH);

            if (khachHang == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Kiểm tra các trường không được để trống
            if (string.IsNullOrEmpty(passold) || string.IsNullOrEmpty(newpass) || string.IsNullOrEmpty(confpass))
            {
                ViewBag.ErrorMessage = "Vui lòng không để trống trường nào.";
                return View();
            }
            // Kiểm tra nếu mật khẩu cũ không khớp

            if (HashPassword(passold) != khachHang.MatKhauKH)
            {
                ViewBag.ErrorMessage = "Mật khẩu cũ không chính xác.";
                return View(khachHang);
            }




            // Kiểm tra nếu mật khẩu mới và xác nhận mật khẩu không khớp
            if (newpass != confpass)
            {
                ViewBag.ErrorMessage = "Mật khẩu xác nhận không khớp.";
                return View();
            }

            string hashedPassword = HashPassword(newpass);

            // Cập nhật mật khẩu mới
            khachHang.MatKhauKH = hashedPassword;

            try
            {
                db.SubmitChanges(); // Lưu thay đổi vào cơ sở dữ liệu
                ViewBag.SuccessMessage = "Đổi mật khẩu thành công!";
            }
            catch (Exception)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi trong quá trình đổi mật khẩu. Vui lòng thử lại.";
                return View("Doi_mk", khachHang);
            }

            return View("Doi_mk", khachHang);
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }
        }

        public ActionResult Gio_hang()
        {
            return View();
        }

        public ActionResult Chi_tiet_SP()
        {
            return View();
        }

        public ActionResult Xoa_bo()
        {
            return View();
        }

        public ActionResult Thanh_toan()
        {
            return View();
        }

        public ActionResult Xoa_gio_hang()
        {
            return View();
        }

        public ActionResult Chi_tiet_DH()
        {
            return View();
        }

        public ActionResult Lich_su_don_hang()
        {
            return View();
        }

        public ActionResult Thong_ke()
        {
            return View();
        }

        public ActionResult Doimatkhau_quen()
        {
            return View();
        }
        public ActionResult Xacnhan_dathangtcong()
        {
            return View();
        }
        public ActionResult Thongke_DH()
        {
            return View();
        }

        // Action đăng xuất
        public ActionResult DangXuat()
        {
            // Xóa session và chuyển hướng về trang đăng nhập
            Session["TenKH"] = null;
            return RedirectToAction("Login", "TaiKhoan");
        }
        // Action xử lý điều hướng khi bấm vào "LAPTOP GO"
        public ActionResult Trang()
        {
            // Kiểm tra xem người dùng đã đăng nhập chưa (Kiểm tra thông tin trong session)
            if (Session["TenKH"] != null)
            {
                // Nếu đã đăng nhập, chuyển đến Trang_KH
                return RedirectToAction("Trang_KH");
            }
            else
            {
                // Nếu chưa đăng nhập, chuyển đến Index
                return RedirectToAction("Index");
            }
        }
    }
}