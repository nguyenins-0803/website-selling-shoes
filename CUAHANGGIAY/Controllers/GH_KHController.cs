using CUAHANGGIAY.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CUAHANGGIAY.Controllers
{
    public class GH_KHController : Controller
    {
        DataDataContext db;

        public GH_KHController()
        {
            string serverName = Environment.MachineName;
            string defaultDbName = "QLSHOPGIAY";

            // Xây dựng connection string
            string connectionString = $"Data Source={serverName}\\MSSQLSERVER01;Initial Catalog={defaultDbName};Integrated Security=True;TrustServerCertificate=True";

            // Khởi tạo DataContext với connection string
            db = new DataDataContext(connectionString);
        }



        public ActionResult Index()
        {
            if (Session["MaKH"] == null)
                return RedirectToAction("Login", "TK_KH");
            return View();
        }

        // Lấy danh sách sản phẩm trong giỏ hàng
        [HttpPost]
        public JsonResult GetCartItems()
        {
            string maKH = Session["MaKH"] as string;
            if (string.IsNullOrEmpty(maKH))
                return Json(new { success = false, message = "Không tìm thấy mã khách hàng!" }, JsonRequestBehavior.AllowGet);

            var cartItems = (from gh in db.GIOHANGs
                             join sp in db.SANPHAMs
                                on gh.MaSP equals sp.MaSP
                             join cs in db.CHITIETSIZEs
                                on new { gh.MaSP, gh.MaSize }
                                equals new { cs.MaSP, cs.MaSize }
                             where gh.MaKH == maKH && cs.SoLuong > 0
                             select new CartItem
                             {
                                 ID = sp.MaSP,
                                 Size = gh.MaSize,
                                 Name = sp.TenSP,
                                 Price = sp.GiaSP,
                                 Quantity = gh.SoLuong,
                                 Color = sp.MauSP,
                                 RemainingStock = cs.SoLuong,
                                 Image = sp.AnhMinhHoa
                             }).ToList();

            return Json(new { success = true, data = cartItems }, JsonRequestBehavior.AllowGet);
        }

        // Thêm sản phẩm vào giỏ hàng (cần truyền size)
        [HttpPost]
        public JsonResult AddToCart(string id, int size)
        {
            string maKH = Session["MaKH"] as string;
            if (string.IsNullOrEmpty(maKH))
                return Json(new { success = false, message = "Vui lòng đăng nhập." });

            // Lấy tồn kho theo size
            var cs = db.CHITIETSIZEs
                       .FirstOrDefault(x => x.MaSP == id && x.MaSize == size && x.SoLuong > 0);
            if (cs == null)
                return Json(new { success = false, message = "Sản phẩm không tồn tại hoặc đã hết hàng (size này)." });

            // Xem đã có trong giỏ
            var gh = db.GIOHANGs
                       .FirstOrDefault(x => x.MaKH == maKH && x.MaSP == id && x.MaSize == size);
            if (gh != null)
            {
                if (gh.SoLuong < cs.SoLuong)
                    gh.SoLuong++;
                else
                    return Json(new { success = false, message = "Số lượng vượt quá tồn kho." });
            }
            else
            {
                gh = new GIOHANG
                {
                    MaKH = maKH,
                    MaSP = id,
                    MaSize = size,
                    SoLuong = 1
                };
                db.GIOHANGs.InsertOnSubmit(gh);
            }

            try
            {
                db.SubmitChanges();
                return Json(new { success = true, message = "Thêm vào giỏ hàng thành công." });
            }
            catch
            {
                return Json(new { success = false, message = "Lỗi khi thêm sản phẩm vào giỏ hàng." });
            }
        }

        // Cập nhật số lượng (cần truyền size)
        [HttpPost]
        public JsonResult UpdateCart(string id, int size, int quantity)
        {
            string maKH = Session["MaKH"] as string;
            if (string.IsNullOrEmpty(maKH))
                return Json(new { success = false, message = "Vui lòng đăng nhập." });

            var cs = db.CHITIETSIZEs
                       .FirstOrDefault(x => x.MaSP == id && x.MaSize == size);
            if (cs == null || cs.SoLuong <= 0)
                return Json(new { success = false, message = "Sản phẩm không tồn tại hoặc đã hết hàng." });

            var gh = db.GIOHANGs
                       .FirstOrDefault(x => x.MaKH == maKH && x.MaSP == id && x.MaSize == size);
            if (gh == null)
                return Json(new { success = false, message = "Sản phẩm không có trong giỏ hàng." });

            if (quantity > cs.SoLuong)
                return Json(new { success = false, message = "Số lượng vượt quá tồn kho." });

            gh.SoLuong = quantity;
            try
            {
                db.SubmitChanges();
                int totalPrice = gh.SoLuong * gh.SANPHAM.GiaSP;
                return Json(new { success = true, totalPrice });
            }
            catch
            {
                return Json(new { success = false, message = "Lỗi khi cập nhật giỏ hàng." });
            }
        }

        // Xóa sản phẩm khỏi giỏ hàng (cần truyền size)
        [HttpPost]
        public JsonResult RemoveFromCart(string id, int size)
        {
            string maKH = Session["MaKH"] as string;
            if (string.IsNullOrEmpty(maKH))
                return Json(new { success = false, message = "Vui lòng đăng nhập." });

            var gh = db.GIOHANGs
                       .FirstOrDefault(x => x.MaKH == maKH && x.MaSP == id && x.MaSize == size);
            if (gh == null)
                return Json(new { success = false, message = "Sản phẩm không có trong giỏ hàng." });

            try
            {
                db.GIOHANGs.DeleteOnSubmit(gh);
                db.SubmitChanges();
                return Json(new { success = true, message = "Xóa sản phẩm khỏi giỏ hàng thành công." });
            }
            catch
            {
                return Json(new { success = false, message = "Lỗi khi xóa sản phẩm khỏi giỏ hàng." });
            }
        }

        // Thanh toán: ghi đơn và chi tiết theo size, cập nhật kho CHITIETSIZE
        [HttpPost]
        public JsonResult Checkout(string fullName, string email, string phone, string address, string paymentMethod)
        {
            try
            {
                string maKH = Session["MaKH"] as string;
                var cartItems = db.GIOHANGs
                                    .Where(gh => gh.MaKH == maKH)
                                    .ToList();
                if (!cartItems.Any())
                    return Json(new { success = false, message = "Giỏ hàng trống." });

                string orderId = "DH-" + DateTime.Now.ToString("yyyyMMddHHmmss");
                var donHang = new DONHANG
                {
                    MaDH = orderId,
                    MaKH = maKH,
                    NgayMua = DateTime.Now,
                    DiaChi = address,
                    ThanhToan = paymentMethod,
                    TrangThai = "Đang xử lý",
                    TongTien = cartItems.Sum(ci => ci.SANPHAM.GiaSP * ci.SoLuong)
                };
                db.DONHANGs.InsertOnSubmit(donHang);
                db.SubmitChanges();

                foreach (var item in cartItems)
                {
                    // Lấy CHITIETSIZE để trừ tồn
                    var cs = db.CHITIETSIZEs
                               .FirstOrDefault(x => x.MaSP == item.MaSP && x.MaSize == item.MaSize);
                    if (cs == null || cs.SoLuong < item.SoLuong)
                        return Json(new { success = false, message = $"Không đủ '{item.SANPHAM.TenSP}' size {item.MaSize}." });

                    cs.SoLuong -= item.SoLuong;

                    var ct = new CHITIETDH
                    {
                        MaDH = orderId,
                        MaSP = item.MaSP,
                        MaSize = item.MaSize,
                        SLMua = item.SoLuong,
                        GiaMua = item.SANPHAM.GiaSP
                    };
                    db.CHITIETDHs.InsertOnSubmit(ct);
                }

                db.GIOHANGs.DeleteAllOnSubmit(cartItems);
                db.SubmitChanges();

                return Json(new { success = true, message = "Đặt hàng thành công." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi đặt hàng: " + ex.Message });
            }
        }
        public ActionResult ThanhToan()
        {
            if (Session["MaKH"] == null)
            {
                return RedirectToAction("Login", "TK_KH");
            }

            string maKH = Session["MaKH"].ToString();

            var cartItems = (from gh in db.GIOHANGs
                             join sp in db.SANPHAMs on gh.MaSP equals sp.MaSP
                             join sz in db.SIZEs on gh.MaSize equals sz.MaSize
                             join ctSize in db.CHITIETSIZEs on new { gh.MaSP, gh.MaSize } equals new { ctSize.MaSP, ctSize.MaSize }
                             where gh.MaKH == maKH
                             select new CartItem
                             {
                                 ID = gh.MaSP,
                                 Name = sp.TenSP,
                                 Price = sp.GiaSP,
                                 Quantity = gh.SoLuong,
                                 Size = gh.MaSize,
                                 Color = sp.MauSP,
                                 RemainingStock = ctSize.SoLuong,
                                 Image = sp.AnhMinhHoa
                             }).ToList();

            if (!cartItems.Any())
            {
                return RedirectToAction("Index", "Home");
            }

            var totalAmount = cartItems.Sum(ci => ci.Price * ci.Quantity);
            ViewData["CartItems"] = cartItems;
            ViewData["TotalAmount"] = totalAmount.ToString("N0") + " VND";

            var user = db.KHACHHANGs.FirstOrDefault(kh => kh.MaKH == maKH);
            if (user != null)
            {
                ViewData["FullName"] = user.TenKH;
                ViewData["Email"] = user.Gmail;
                ViewData["Phone"] = user.SDT;
                ViewData["Address"] = user.DiaChi;
            }

            return View();
        }


        // (Giữ nguyên GenerateOrderId nếu cần)
    }
}
