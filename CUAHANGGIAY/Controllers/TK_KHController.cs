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
    public class TK_KHController : Controller
    {
        // GET: TK_KH
        DataDataContext db;

        public TK_KHController()
        {
            string serverName = Environment.MachineName;
            string defaultDbName = "QLSHOPGIAY";

            // Xây dựng connection string
            string connectionString = $"Data Source={serverName}\\SQLEXPRESS;Initial Catalog={defaultDbName};Integrated Security=True;TrustServerCertificate=True";

            // Khởi tạo DataContext với connection string
            db = new DataDataContext(connectionString);
        }

        // GET: Hiển thị trang đăng ký
        public ActionResult Index()
        {
            return View();
        }


        public ActionResult DangKy()
        {
            return View();
        }

        // POST: Xử lý đăng ký tài khoản khách hàng
        [HttpPost]
        public JsonResult DangKyXuLy(string tenKH, string sdt, string matKhauKH, string xacNhanMatKhau, string diaChi, string gmail)
        {
            // Kiểm tra các trường không được để trống
            if (string.IsNullOrEmpty(tenKH) || string.IsNullOrEmpty(sdt) || string.IsNullOrEmpty(matKhauKH) ||
                string.IsNullOrEmpty(xacNhanMatKhau) || string.IsNullOrEmpty(diaChi) || string.IsNullOrEmpty(gmail))
            {
                return Json(new { success = false, message = "Vui lòng không để trống trường nào." });
            }

            // Kiểm tra định dạng số điện thoại
            var phoneRegex = new Regex(@"^0\d{9}$");
            if (!phoneRegex.IsMatch(sdt))
            {
                return Json(new { success = false, message = "Số điện thoại không hợp lệ. Vui lòng nhập số có 10 chữ số và bắt đầu bằng số 0." });
            }

            // Kiểm tra định dạng email
            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            if (!emailRegex.IsMatch(gmail))
            {
                return Json(new { success = false, message = "Email không hợp lệ. Vui lòng nhập email đúng định dạng." });
            }
            //Kiem tra ton tai sdt 
            if (db.KHACHHANGs.Any(k => k.SDT == sdt))
            {
                return Json(new { success = false, message = "Số điện thoại đã được sử dụng." });
            }
            // Kiểm tra mật khẩu và xác nhận mật khẩu
            if (matKhauKH != xacNhanMatKhau)
            {
                return Json(new { success = false, message = "Mật khẩu và xác nhận mật khẩu không khớp." });
            }

            // Mã hóa mật khẩu
            string hashedPassword = HashPassword(matKhauKH);


            // Thêm khách hàng vào database
            var newCustomer = new KHACHHANG
            {
                MaKH = Guid.NewGuid().ToString(),  // Tạo mã khách hàng tự động
                MaRole = true, // Giả sử true là vai trò khách hàng
                TenKH = tenKH,
                SDT = sdt,
                MatKhauKH = hashedPassword,
                DiaChi = diaChi,
                Gmail = gmail
            };

            db.KHACHHANGs.InsertOnSubmit(newCustomer);
            db.SubmitChanges();

            return Json(new { success = true, message = "Đăng ký thành công!" });
        }



        // GET: Hiển thị trang đăng nhập
        public ActionResult Login()
        {
            return View();
        }

        // POST: Xử lý đăng nhập
        [HttpPost]
        public JsonResult LoginXuLy(string taiKhoan, string matKhau)
        {
            var hashedPassword = HashPassword(matKhau);

            var khachHang = db.KHACHHANGs.FirstOrDefault(k => k.SDT == taiKhoan);
            var admin = db.ADMINs.FirstOrDefault(a => a.TaiKhoanAdmin == taiKhoan);

            if (khachHang != null)
            {
                // Kiểm tra nếu mật khẩu đã được băm
                if (khachHang.MatKhauKH == hashedPassword)
                {
                    Session["UserRole"] = " Khách Hàng ";
                    Session["TenKH"] = khachHang.TenKH;
                    Session["MaKH"] = khachHang.MaKH;

                    return Json(new { success = true, message = "Đăng nhập thành công!", redirectUrl = "/Home/Index" });
                }
                // Nếu mật khẩu chưa được băm, so sánh với mật khẩu rõ
                else if (khachHang.MatKhauKH == matKhau)
                {
                    // Cập nhật mật khẩu băm vào cơ sở dữ liệu
                    khachHang.MatKhauKH = hashedPassword;
                    db.SubmitChanges();

                    Session["UserRole"] = "Khách Hàng";
                    Session["TenKH"] = khachHang.TenKH;
                    Session["MaKH"] = khachHang.MaKH;
                    return Json(new { success = true, message = "Đăng nhập thành công!", redirectUrl = "/Home/Index" });
                }
            }
            else if (admin != null)
            {
                // Kiểm tra nếu mật khẩu đã được băm
                if (admin.MatKhauAdmin == hashedPassword)
                {
                    Session["UserRole"] = "Admin";
                    Session["TenAdmin"] = admin.TenAdmin;
                    Session["TaiKhoanAdmin"] = admin.TaiKhoanAdmin;

                    return Json(new { success = true, message = "Đăng nhập thành công!", redirectUrl = "/SanPham/Index" });
                }
                // Nếu mật khẩu chưa được băm, so sánh với mật khẩu rõ
                else if (admin.MatKhauAdmin == matKhau)
                {
                    // Cập nhật mật khẩu băm vào cơ sở dữ liệu
                    admin.MatKhauAdmin = hashedPassword;
                    db.SubmitChanges();

                    Session["UserRole"] = "Admin";
                    return Json(new { success = true, message = "Đăng nhập thành công!", redirectUrl = "/SanPham/Index" });
                }
            }

            return Json(new { success = false, message = "Tên đăng nhập hoặc mật khẩu không đúng." });
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }
        }
        public ActionResult Quen_MK()
        {
            return View();
        }

        // POST: Gửi OTP
        [HttpPost]
        public JsonResult SendOTP(string sdt)
        {
            if (string.IsNullOrEmpty(sdt))
            {
                return Json(new { success = false, message = "Số điện thoại không được để trống." });
            }

            // Kiểm tra số điện thoại có trong hệ thống
            var khachHang = db.KHACHHANGs.FirstOrDefault(k => k.SDT == sdt);
            if (khachHang == null)
            {
                return Json(new { success = false, message = "Số điện thoại không tồn tại trong hệ thống." });
            }

            // Kiểm tra thời gian gửi mã OTP trước đó
            if (khachHang.ResetCodeSentTime != null && (DateTime.Now - khachHang.ResetCodeSentTime.Value).TotalSeconds < 60)
            {
                return Json(new { success = false, message = "Bạn chỉ có thể yêu cầu mã OTP sau 60 giây." });
            }

            // Tạo mã OTP
            var otp = GenerateRandomOTP(6);
            khachHang.ResetCode = otp;
            khachHang.ResetCodeSentTime = DateTime.Now;
            db.SubmitChanges();

            // Giả lập gửi OTP qua console (thực tế sẽ gửi qua SMS hoặc email)
            Console.WriteLine($"[Mã OTP]: {otp} đã gửi đến số điện thoại {sdt}");

            return Json(new { success = true, message = "Mã OTP đã được gửi thành công." });
        }

        // Hàm tạo mã OTP ngẫu nhiên
        private string GenerateRandomOTP(int length)
        {
            var random = new Random();
            return new string(Enumerable.Repeat("0123456789", length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        // POST: Xác nhận mã OTP và chuyển đến trang Đặt lại mật khẩu
        [HttpPost]


        public JsonResult VerifyOTP(string sdt, string maOTP)
        {
            var khachHang = db.KHACHHANGs.FirstOrDefault(k => k.SDT == sdt);

            if (khachHang == null)
            {
                return Json(new { success = false, message = "Số điện thoại không tồn tại trong hệ thống." });
            }

            // Kiểm tra mã OTP
            if (khachHang.ResetCode != maOTP)
            {
                return Json(new { success = false, message = "Mã OTP không chính xác." });
            }

            // Nếu mã OTP chính xác, trả về đường dẫn tới trang Dat_lai_mk
            Session["sdt"] = khachHang.SDT;
            return Json(new { success = true, redirectUrl = Url.Action("Dat_lai_mk", "TK_KH") });
        }

        public ActionResult Dat_lai_mk()
        {
            // Kiểm tra session
            var phone = Session["sdt"] as string;

            if (string.IsNullOrEmpty(phone))
            {
                TempData["ErrorMessage"] = "Bạn cần xác thực thông tin trước khi truy cập!";
                return RedirectToAction("Login", "TK_KH"); // Chuyển về trang login
            }

            // Lấy thông tin khách hàng từ số điện thoại
            var customer = db.KHACHHANGs.SingleOrDefault(c => c.SDT == phone);
            if (customer == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khách hàng với số điện thoại này!";
                return RedirectToAction("Login", "TK_KH");
            }

            // Truyền thông tin khách hàng vào view
            return View();
        }

        [HttpPost]
        public ActionResult Dat_lai_mk(string newPassword, string confirmPassword)
        {
            // Kiểm tra nếu mật khẩu mới và mật khẩu xác nhận không giống nhau
            if (newPassword != confirmPassword)
            {
                ViewBag.ErrorMessage = "Mật khẩu và xác nhận mật khẩu không khớp!";
                return View();
            }

            // Kiểm tra điều kiện bảo mật của mật khẩu
            if (newPassword.Length < 6)
            {
                ViewBag.ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự!";
                return View();
            }

            try
            {
                // Lấy số điện thoại từ session
                string phone = Session["sdt"]?.ToString();

                if (string.IsNullOrEmpty(phone))
                {
                    TempData["ErrorMessage"] = "Phiên làm việc không hợp lệ, vui lòng đăng nhập lại!";
                    return RedirectToAction("Login", "TK_KH");
                }

                // Tìm khách hàng trong cơ sở dữ liệu
                var customer = db.KHACHHANGs.SingleOrDefault(c => c.SDT == phone);
                if (customer == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy khách hàng với số điện thoại này!";
                    return RedirectToAction("Login", "TK_KH");
                }

                // Cập nhật mật khẩu khách hàng
                customer.MatKhauKH = HashPassword(newPassword); // Mã hóa mật khẩu
                db.SubmitChanges();

                // Xóa session để bảo mật và chuyển về trang login
                Session.Remove("sdt");
                TempData["SuccessMessage"] = "Đổi mật khẩu thành công! Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "TK_KH");
            }
            catch (Exception)
            {
                ViewBag.ErrorMessage = "Có lỗi xảy ra, vui lòng thử lại!";
            }

            return View();
        }

    }
}