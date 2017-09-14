$(document).ready(function () {
    $("#btnnext").click(function () {
        debugger;
        if ($('#txtloan_granted').val() === 0 || $('#txtloan_granted').val() === "") {
            alert("Please input loan amount.");
        }
        else if ($('#txtloantype_id').val() === 0 || $('#txtloantype_id').val() === "") {
            alert("Please input loan type.");
        }
        else {
            CreateSchedule();
            $('.nav-pills a[href="#step2"]').tab('show');
        }
    });
    $("#btnnext1").click(function () {
        if ($('#txtloan_granted').val() == 0) {
            alert("Please input loan amount.");
        }
        else {
            $('.nav-pills a[href="#step3"]').tab('show');
        }
    });
});