
/***************** RTL *********************/
$('#myonoffswitch55').click(function () {
    if (this.checked) {
        directionCheck("rtl");

    } 
});
/***************** RTL *********************/

/***************** LTR *********************/

$('#myonoffswitch54').click(function () {
    if (this.checked) {
        directionCheck("ltr");
    } 
});
/***************** LTR *********************/

$(function () {
    let bodyRtl = $('body').hasClass('rtl');
    if (bodyRtl) {
        directionCheck("rtl");
    }

})