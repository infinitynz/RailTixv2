(function (window, $) {
    'use strict';

    function LocationBootstrap(opts) {
        this.enabled = !!opts || true;
        this.isAuthenticated = !!opts.isAuthenticated;
        this.updateUrl = opts.updateUrl;
        this.guessUrl = opts.guessUrl;
    }

    LocationBootstrap.prototype.init = function () {
        if (this.isAuthenticated) return; // profile already known
        this.ensureLocation();
    };

    LocationBootstrap.prototype.ensureLocation = function () {
        var self = this;
        if (navigator.geolocation) {
            navigator.geolocation.getCurrentPosition(function (pos) {
                $.ajax({
                    url: self.updateUrl,
                    method: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify({ latitude: pos.coords.latitude, longitude: pos.coords.longitude })
                });
            }, function () {
                $.ajax({ url: self.guessUrl, method: 'GET' });
            }, { maximumAge: 600000, timeout: 8000 });
        } else {
            $.ajax({ url: self.guessUrl, method: 'GET' });
        }
    };

    window.RailTixLocation = {
        init: function (opts) {
            var lb = new LocationBootstrap(opts);
            lb.init();
        }
    };
})(window, window.jQuery);


