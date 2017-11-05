app.loanDetail = function () {
    "use strict";

    var self = this;

    // #region MODEL TO CREATE/UPDATE
    self.autonum = ko.observable();
    self.customer_name = ko.observable();
    self.loan_no = ko.observable();
    self.loan_granted = ko.observable();
    self.loan_interest_rate = ko.observable();
    self.payment_scheme = ko.observable();
    self.due_date = ko.observable();
    self.loan_date = ko.observable();
    self.installment_no = ko.observable();
    self.total_receivables = ko.observable();
    self.balance = ko.observable();
    // #endregion   

    return self;
};