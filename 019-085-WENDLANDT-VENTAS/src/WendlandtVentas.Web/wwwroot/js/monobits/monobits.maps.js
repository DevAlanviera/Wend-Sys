var map;
var geocoder;
var marker;
var originalPosition;

function initializeMap() {
    originalPosition = new google.maps.LatLng(32.77428035254273, -115.51078715624999);
    geocoder = new google.maps.Geocoder();

    var mapOptions = {
        zoom: 8,
        center: originalPosition
    };

    map = new google.maps.Map(document.getElementById("map-canvas"),
        mapOptions);
}

function putMarker() {
    if (originalPosition === null) {
        originalPosition = new google.maps.LatLng(32.77428035254273, -115.51078715624999);
    }
    marker = new google.maps.Marker({
        position: originalPosition,
        map: map,
        animation: google.maps.Animation.DROP,
        draggable: true,
        title: "Move me to the right position"
    });

    marker.setMap(map);
}

function codeAddress(address) {
    geocoder.geocode({ 'address': address },
        function(results, status) {
            if (status === google.maps.GeocoderStatus.OK) {
                originalPosition = results[0].geometry.location;
                map.setZoom(15);
                map.setCenter(originalPosition);
                marker.setPosition(originalPosition);
                onCodeAddressSuccess();
                return true;
            } else {
                return false;
            }
        });
}

function onCodeAddressSuccess() {
    return true;
}