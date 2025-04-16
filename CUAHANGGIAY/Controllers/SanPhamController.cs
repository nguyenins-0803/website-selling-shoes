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
            // Lấy tên máy chủ hiện tại
            string serverName = Environment.MachineName;
            string defaultDbName = "QLSHOPGIAY";

            // Xây dựng connection string
            string connectionString = $"Data Source=YENNGTH-0803\\MSSQLSERVER01;Initial Catalog=QLSHOPGIAY;Integrated Security=True";

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

        public ActionResult Create()
        {
            if (Session["TaiKhoanAdmin"] == null)
            {
                return RedirectToAction("Login", "TK_KH");
            }
            return View();
        }
        //public ActionResult Details(string id)
        //{
        //    if (Session["MaKH"] == null)
        //    {
        //        return RedirectToAction("Login", "TK_KH");
        //    }

        //    var product = _sanPhamContext.SANPHAMs
        //        .FirstOrDefault(sp => sp.MaSP == id && sp.IsDeleted == false);

        //    if (product == null)
        //    {
        //        return HttpNotFound();
        //    }

        //    var sizes = _sanPhamContext.CHITIETSIZEs
        //        .Where(ct => ct.MaSP == id && ct.SoLuong > 0)
        //        .Select(ct => new
        //        {
        //            ct.SIZE.MaSize,
        //            ct.SIZE.TenSize,
        //            ct.SoLuong
        //        }).ToList();

        //    ViewBag.ListSize = sizes;
        //    return View(product);
        //}



        [Route("SanPham/Edit/{id}")]
        public ActionResult Edit(string id)
        {
            if (Session["TaiKhoanAdmin"] == null)
            {
                return RedirectToAction("Login", "TK_KH");
            }
            var product = db.SANPHAMs.FirstOrDefault(x => x.MaSP == id && x.IsDeleted == false);
            if (product == null)
            {
                return HttpNotFound();
            }
            return View(product);
        }

        public JsonResult PostCreate()
        {
            try
            {
                // Lấy các giá trị từ form
                string tenSanPham = Request.Form["TenSanPham"];
                string idDanhMuc = Request.Form["IdDanhMuc"];
                string moTa = Request.Form["MoTa"];
                string gia = Request.Form["Gia"];
                string mauSP = Request.Form["MauSP"];
                string thuongHieu = Request.Form["ThuongHieu"];
                string chatLieu = Request.Form["ChatLieu"];
                string kieuDang = Request.Form["KieuDang"];

                // Lấy thông tin size và số lượng từ form
                var sizeList = Request.Form["Size"]; // Giả sử truyền mảng các size (ví dụ: ["38", "39", "40"])
                var soLuongList = Request.Form["SoLuong"]; // Giả sử truyền mảng các số lượng (ví dụ: ["10", "15", "5"])

                // Kiểm tra các trường bắt buộc
                if (string.IsNullOrWhiteSpace(tenSanPham) ||
                    string.IsNullOrWhiteSpace(idDanhMuc) ||
                    string.IsNullOrWhiteSpace(moTa) ||
                    string.IsNullOrWhiteSpace(gia) ||
                    string.IsNullOrWhiteSpace(mauSP) ||
                    string.IsNullOrWhiteSpace(thuongHieu) ||
                    string.IsNullOrWhiteSpace(chatLieu) ||
                    string.IsNullOrWhiteSpace(kieuDang) ||
                    sizeList == null || sizeList.Length == 0 || soLuongList == null || soLuongList.Length == 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Vui lòng điền đầy đủ tất cả các trường"
                    }, JsonRequestBehavior.AllowGet);
                }

                // Convert giá trị số
                int giaSP = Convert.ToInt32(gia);

                // Lấy file hình ảnh từ request
                var imageFile = Request.Files["ImageFile"];

                // Kiểm tra nếu không có file
                if (imageFile == null || imageFile.ContentLength == 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Không có hình ảnh được chọn"
                    }, JsonRequestBehavior.AllowGet);
                }

                // Tạo đối tượng mới cho sản phẩm
                SANPHAM newObj = new SANPHAM
                {
                    TenSP = tenSanPham,
                    MaDM = idDanhMuc,
                    GiaSP = giaSP,
                    Mota = moTa,
                    MauSP = mauSP,
                    ThuongHieu = thuongHieu,
                    ChatLieu = chatLieu,
                    KieuDang = kieuDang,
                    IsDeleted = false, // Sản phẩm mới mặc định chưa bị xóa
                };

                // Kiểm tra thư mục lưu hình ảnh
                var imageFolderPath = Server.MapPath("~/Images/");
                if (!Directory.Exists(imageFolderPath))
                {
                    Directory.CreateDirectory(imageFolderPath);
                }

                // Lưu file vào thư mục "Images"
                var fileName = Path.GetFileName(imageFile.FileName);
                var filePath = Path.Combine(imageFolderPath, fileName);
                imageFile.SaveAs(filePath);

                // Lưu đường dẫn hình ảnh vào database
                newObj.AnhMinhHoa = "/Images/" + fileName;

                // Lưu sản phẩm vào cơ sở dữ liệu
                db.SANPHAMs.InsertOnSubmit(newObj);
                db.SubmitChanges();

                // Lưu các size và số lượng vào bảng CHITIETSIZE
                for (int i = 0; i < sizeList.Length; i++)
                {
                    int maSize = Convert.ToInt32(sizeList[i]);
                    int soLuong = Convert.ToInt32(soLuongList[i]);

                    // Tạo đối tượng chi tiết size và lưu vào bảng CHITIETSIZE
                    CHITIETSIZE chiTietSize = new CHITIETSIZE
                    {
                        MaSP = newObj.MaSP, // Lấy mã sản phẩm đã tạo
                        MaSize = maSize,
                        SoLuong = soLuong,
                    };

                    db.CHITIETSIZEs.InsertOnSubmit(chiTietSize);
                }

                db.SubmitChanges();

                return Json(new
                {
                    success = true,
                    message = "Thêm mới thành công"
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Có lỗi xảy ra: " + ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }



        //[HttpPost]
        //public JsonResult PostEdit(FormCollection form)
        //{
        //    try
        //    {
        //        // Lấy các giá trị từ form
        //        string maSanPham = Request.Form["MaSanPham"];
        //        string tenSanPham = Request.Form["TenSanPham"];
        //        string idDanhMuc = Request.Form["IdDanhMuc"];
        //        string moTa = Request.Form["MoTa"];
        //        string soLuong = Request.Form["SoLuong"];
        //        int soLuongInt = Convert.ToInt32(soLuong);
        //        string mauSP = Request.Form["MauSP"];
        //        string gia = Request.Form["Gia"];
        //        int giaSP = Convert.ToInt32(gia);
        //        string ram = Request.Form["RAM"];
        //        string ssd = Request.Form["SSD"];
        //        string cpu = Request.Form["CPU"];
        //        string manHinh = Request.Form["ManHinh"];
        //        string card = Request.Form["Card"];
        //        string pin = Request.Form["Pin"];


        //        var product = _sanPhamContext.SANPHAMs.FirstOrDefault(x => x.MaSP == maSanPham && x.IsDeleted == false);
        //        if (product != null)
        //        {
        //            product.TenSP = tenSanPham;
        //            product.MaDM = idDanhMuc;
        //            product.GiaSP = giaSP;
        //            product.Mota = moTa;
        //            product.SoLuong = soLuongInt;
        //            product.MauSP = mauSP;
        //            product.RAM = ram;
        //            product.SSD = ssd;
        //            product.CPU = cpu;
        //            product.ManHinh = manHinh;
        //            product.Card = card;
        //            product.Pin = pin;

        //            var imageFile = Request.Files["ImageFile"];

        //            // Chỉ cập nhật hình ảnh nếu có ảnh mới
        //            if (imageFile != null && imageFile.ContentLength > 0)
        //            {
        //                // Kiểm tra thư mục lưu hình ảnh
        //                var imageFolderPath = Server.MapPath("~/Images/");
        //                if (!Directory.Exists(imageFolderPath))
        //                {
        //                    Directory.CreateDirectory(imageFolderPath);
        //                }

        //                // Lưu file vào thư mục "Images"
        //                var fileName = Path.GetFileName(imageFile.FileName);
        //                var filePath = Path.Combine(imageFolderPath, fileName);
        //                imageFile.SaveAs(filePath);

        //                // Cập nhật đường dẫn hình ảnh vào cơ sở dữ liệu
        //                product.AnhMinhHoa = "/Images/" + fileName;
        //            }

        //            // Cập nhật sản phẩm vào cơ sở dữ liệu
        //            _sanPhamContext.SubmitChanges();

        //            return Json(new
        //            {
        //                success = true,
        //                message = "Cập nhật sản phẩm thành công"
        //            }, JsonRequestBehavior.AllowGet);
        //        }

        //        return Json(new
        //        {
        //            success = false,
        //            message = "Sản phẩm không tồn tại."
        //        }, JsonRequestBehavior.AllowGet);
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new
        //        {
        //            success = false,
        //            message = "Có lỗi xảy ra: " + ex.Message
        //        }, JsonRequestBehavior.AllowGet);
        //    }
        //}

        //[HttpPost]
        //public JsonResult XoaSanPham(string id)
        //{
        //    // Lấy sản phẩm theo mã sản phẩm
        //    var product = _sanPhamContext.SANPHAMs.FirstOrDefault(x => x.MaSP == id && x.IsDeleted == false);

        //    if (product == null)
        //    {
        //        return Json(new { success = false, message = "Sản phẩm không tồn tại." });
        //    }

        //    // Kiểm tra xem sản phẩm có tồn tại trong đơn hàng có trạng thái "Đang xử lý" hoặc "Đang giao"
        //    var orders = _sanPhamContext.CHITIETDHs
        //        .Where(x => x.MaSP == id)  // Lọc chi tiết đơn hàng theo mã sản phẩm
        //        .Join(_sanPhamContext.DONHANGs,  // Join với bảng DONHANG
        //            chiTiet => chiTiet.MaDH,     // Liên kết với MaDH từ CHITIETDHs
        //            donHang => donHang.MaDH,     // Liên kết với MaDH từ DONHANG
        //            (chiTiet, donHang) => new { chiTiet, donHang })  // Kết hợp cả hai bảng
        //        .Where(result => result.donHang.TrangThai == "Đang xử lý" || result.donHang.TrangThai == "Đang giao")  // Kiểm tra trạng thái "Đang xử lý" hoặc "Đang giao"
        //        .Select(result => result.donHang)  // Chọn đối tượng đơn hàng
        //        .ToList();  // Chuyển kết quả thành danh sách

        //    // Nếu tồn tại đơn hàng có trạng thái "Đang xử lý" hoặc "Đang giao", không cho phép xóa
        //    if (orders.Any())
        //    {
        //        return Json(new { success = false, message = "Sản phẩm không thể xóa vì đang tồn tại trong đơn hàng có trạng thái 'Đang xử lý' hoặc 'Đang giao'." });
        //    }
        //    else
        //    {
        //        // Nếu không có đơn hàng hoặc tất cả đơn hàng có trạng thái hợp lệ, đánh dấu sản phẩm là đã xóa (IsDeleted = true)
        //        product.IsDeleted = true;
        //        _sanPhamContext.SubmitChanges();
        //    }

        //    return Json(new { success = true, message = "Sản phẩm đã được xóa thành công." });
        //}


        //public JsonResult GetListSanPham(int pageNumber, int pageSize)
        //{
        //    // Truy vấn kết hợp giữa bảng SANPHAMs và DANHMUCs, chỉ lấy sản phẩm chưa bị xóa
        //    var listSanPham = _sanPhamContext.SANPHAMs
        //        .Where(sp => sp.IsDeleted == false)  // Lọc chỉ lấy sản phẩm chưa bị xóa
        //        .Join(_sanPhamContext.DANHMUCs,  // Thực hiện JOIN với bảng DANHMUCs
        //              sp => sp.MaDM,
        //              pc => pc.MaDM,
        //              (sp, pc) => new
        //              {
        //                  sp.MaSP,
        //                  sp.TenSP,
        //                  pc.TenDM,
        //                  sp.RAM,
        //                  sp.CPU,
        //                  sp.SSD,
        //                  sp.MauSP,
        //                  sp.GiaSP,
        //                  sp.SoLuong,
        //                  sp.AnhMinhHoa,
        //                  sp.IsDeleted,
        //              })
        //        .OrderBy(sp => sp.MaSP)
        //        .Skip((pageNumber - 1) * pageSize)  // Thực hiện phân trang
        //        .Take(pageSize)  // Giới hạn số lượng sản phẩm theo pageSize
        //        .ToList();

        //    var tongSoSanPham = _sanPhamContext.SANPHAMs
        //        .Where(sp => sp.IsDeleted == false)
        //        .Count();

        //    return Json(new { listSanPham, tongSoSanPham }, JsonRequestBehavior.AllowGet);
        //}

        //public JsonResult SearchProductsByName(string name, int pageNumber, int pageSize)
        //{
        //    // Kiểm tra nếu 'name' rỗng, không tìm kiếm
        //    if (string.IsNullOrEmpty(name))
        //    {
        //        return Json(new { listSanPham = new List<object>(), tongSoSanPham = 0 }, JsonRequestBehavior.AllowGet);
        //    }

        //    // Lọc sản phẩm theo tên và kiểm tra IsDeleted = false, tìm kiếm không phân biệt chữ hoa chữ thường
        //    var productList = _sanPhamContext.SANPHAMs
        //        .Where(sp => sp.TenSP.ToLower().Contains(name.ToLower()) && sp.IsDeleted == false) // Không phân biệt chữ hoa chữ thường
        //        .Join(_sanPhamContext.DANHMUCs, // JOIN với bảng DANHMUCs
        //            sp => sp.MaDM,
        //            dm => dm.MaDM,
        //            (sp, dm) => new
        //            {
        //                sp.MaSP,
        //                sp.TenSP,
        //                dm.TenDM,
        //                sp.RAM,
        //                sp.CPU,
        //                sp.SSD,
        //                sp.MauSP,
        //                sp.GiaSP,
        //                sp.SoLuong,
        //                sp.AnhMinhHoa,
        //                sp.IsDeleted,
        //            })
        //        .OrderBy(sp => sp.TenSP)  // Sắp xếp theo tên sản phẩm
        //        .Skip((pageNumber - 1) * pageSize)  // Bỏ qua các bản ghi đã được lấy ở các trang trước
        //        .Take(pageSize)  // Lấy số bản ghi theo pageSize
        //        .ToList();

        //    // Lấy tổng số sản phẩm tìm thấy (chưa bị xóa)
        //    var totalSANPHAMs = _sanPhamContext.SANPHAMs
        //        .Where(sp => sp.TenSP.ToLower().Contains(name.ToLower()) && sp.IsDeleted == false)
        //        .Count();

        //    return Json(new { listSanPham = productList, tongSoSanPham = totalSANPHAMs }, JsonRequestBehavior.AllowGet);
        //}


        //public JsonResult GetProductDetail(string id)
        //{
        //    try
        //    {
        //        // Lấy chi tiết sản phẩm theo mã sản phẩm và kiểm tra nếu sản phẩm chưa bị xóa (IsDeleted = false)
        //        var product = _sanPhamContext.SANPHAMs
        //            .Where(sp => sp.MaSP == id && sp.IsDeleted == false)  // Thêm điều kiện kiểm tra IsDeleted
        //            .Join(_sanPhamContext.DANHMUCs,
        //                  sp => sp.MaDM,
        //                  dm => dm.MaDM,
        //                  (sp, dm) => new
        //                  {
        //                      sp.MaSP,
        //                      sp.TenSP,
        //                      dm.TenDM,
        //                      sp.MauSP,
        //                      sp.RAM,
        //                      sp.SSD,
        //                      sp.CPU,
        //                      sp.GiaSP,
        //                      sp.SoLuong,
        //                      sp.AnhMinhHoa,
        //                      sp.Mota
        //                  })
        //            .FirstOrDefault();

        //        if (product == null)
        //        {
        //            return Json(new { success = false, message = "Sản phẩm không tồn tại hoặc đã bị xóa." }, JsonRequestBehavior.AllowGet);
        //        }

        //        return Json(new { success = true, product }, JsonRequestBehavior.AllowGet);
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message }, JsonRequestBehavior.AllowGet);
        //    }



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


    