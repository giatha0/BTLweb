using API_NganLuong;
using ShopBaLoTuiXach.Models;
using ShopBaLoTuiXach.nganluonAPI;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace ShopBaLoTuiXach.Controllers
{
    public class CheckoutController : BaseController
    {
        private const string SessionCart = "SessionCart";
        ShopBaLoTuiXachDbContext db = new ShopBaLoTuiXachDbContext();

        public ActionResult Index()
        {
            var cart = Session[SessionCart];
            var list = new List<Cart_item>();
            if (cart != null)
            {
                list = (List<Cart_item>)cart;
            }
            return View(list);

        }
        [HttpPost]
        public ActionResult Index(Morder order)
        {
            string sumOrder = Request["sumOrder"];
            string payment_method = Request["option_payment"];
            if (payment_method.Equals("COD")) {
                // cap nhat thong tin sau khi dat hang thanh cong
                saveOrder(order);
                var cart = Session[SessionCart];
                var list = new List<Cart_item>();
                ViewBag.cart = (List<Cart_item>)cart;
                Session["SessionCart"] = null;
                var listProductOrder = db.Orderdetails.Where(m => m.orderid == order.ID);
                return View("payment", listProductOrder.ToList());
            }
            string str_bankcode = Request["bankcode"];
            RequestInfo info = new RequestInfo();
            info.Merchant_id = nganluongInfo.Merchant_id;
            info.Merchant_password = nganluongInfo.Merchant_password;
            info.Receiver_email = nganluongInfo.Receiver_email;
            info.cur_code = "vnd";
            info.bank_code = str_bankcode;
            info.Order_code = "DH_6437";
            info.Total_amount = sumOrder;
            info.fee_shipping = "0";
            info.Discount_amount = "0";
            info.order_description = "Thanh toán ngân lượng cho đơn hàng";
            info.return_url = "http://ShopBaLoTuiXachhung.somee.com/confirm-orderPaymentOnline";
            info.cancel_url = "http://localhost:22222/cancel-order";

            info.Buyer_fullname = order.deliveryname;
            info.Buyer_email = order.deliveryemail;
            info.Buyer_mobile = order.deliveryphone;

            APICheckoutV3 objNLChecout = new APICheckoutV3();
            ResponseInfo result = objNLChecout.GetUrlCheckout(info, payment_method);

            if (result.Error_code == "00")
            {

                return Redirect(result.Checkout_url);
            }
            else
            {
                ViewBag.errorPaymentOnline = result.Description;
                return View("payment");
            }

        }
        public ActionResult cancel_order(){

            return View("cancel_order");
        }
        public ActionResult confirm_orderPaymentOnline() {

            String Token = Request["token"];
            RequestCheckOrder info = new RequestCheckOrder();
            info.Merchant_id = nganluongInfo.Merchant_id;
            info.Merchant_password = nganluongInfo.Merchant_password;
            info.Token = Token;
            APICheckoutV3 objNLChecout = new APICheckoutV3();
            ResponseCheckOrder result = objNLChecout.GetTransactionDetail(info);
            if (result.errorCode=="00")
            {

                ViewBag.status = true;
            }
            else
            {
                 ViewBag.status = false;
            }

            return View("confirm_orderPaymentOnline");
        }

        //function ssave order when order success!
        public void saveOrder(Morder order)
        {
            var cart = Session[SessionCart];
            var list = new List<Cart_item>();
            if (cart != null)
            {
                list = (List<Cart_item>)cart;
            }
           
            if (ModelState.IsValid)
            {

                order.code = 1;
                order.userid = 1;
                order.created_ate = DateTime.Now;
                order.updated_by = 1;
                order.updated_at = DateTime.Now;
                order.updated_by = 1;
                order.status = 2;
                order.exportdate = DateTime.Now;
                db.Orders.Add(order);
                db.SaveChanges();

                ViewBag.name = order.deliveryname;
                ViewBag.email = order.deliveryemail;
                ViewBag.address = order.deliveryaddress;
                ViewBag.code = order.code;
                ViewBag.phone = order.deliveryphone;
                Mordersdetail orderdetail = new Mordersdetail();

                foreach (var item in list)
                {
                    float price = 0;
                    int sale = (int)item.product.pricesale;
                    if (sale > 0)
                    {
                        price = (float)item.product.price - (int)item.product.price / 100 * (int)sale * item.quantity;
                    }
                    else
                    {
                        price = (float)item.product.price * (int)item.quantity;
                    }
                    orderdetail.orderid = order.ID;
                    orderdetail.productid = item.product.ID;
                    orderdetail.priceSale = (int)item.product.pricesale;
                    orderdetail.price = item.product.price;
                    orderdetail.quantity = item.quantity;
                    orderdetail.amount = price;

                    db.Orderdetails.Add(orderdetail);
                    db.SaveChanges();
                    //ViewBag.sump = list.Sum((Func<Cart_item, int>)(m => (int)m.product.price * (int) m.quantity));
                    // change number product         
                    var updatedProduct = db.Products.Find(item.product.ID);
                    updatedProduct.catid = item.product.catid;
                    updatedProduct.Submenu = item.product.Submenu;
                    updatedProduct.name = item.product.name;
                    updatedProduct.slug = item.product.slug;
                    updatedProduct.img = item.product.img;
                    updatedProduct.detail = item.product.detail;
                    updatedProduct.number = (int)updatedProduct.number - (int)item.quantity;
                    updatedProduct.pricesale = item.product.pricesale;
                    updatedProduct.price = item.product.price;
                    updatedProduct.metakey = item.product.metakey;
                    updatedProduct.metadesc = item.product.metadesc;
                    updatedProduct.created_by = item.product.created_by;
                    updatedProduct.created_at = item.product.created_at;
                    updatedProduct.updated_by = item.product.updated_by;
                    updatedProduct.updated_at = item.product.updated_at;
                    updatedProduct.status = item.product.status;
                    db.Products.Attach(updatedProduct);
                    db.Entry(updatedProduct).State = EntityState.Modified;
                    db.SaveChanges();
                }
                
            }
        }
        //
    }
}