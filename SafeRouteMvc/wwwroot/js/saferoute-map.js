(function () {
  const target = document.getElementById("safeRouteMap");

  if (!target || !window.safeRouteData) {
    return;
  }

  function riskColor(score) {
    if (score >= 55) return "#c4483e";
    if (score >= 40) return "#b97617";
    return "#16865d";
  }

  function renderGoogleMap() {
    const googleMaps = window.google && window.google.maps;

    if (!googleMaps) {
      return false;
    }

    const map = new googleMaps.Map(target, {
      center: { lat: -26.22, lng: 28.0 },
      zoom: 11,
      mapTypeControl: false,
      streetViewControl: false,
      fullscreenControl: true
    });

    const bounds = new googleMaps.LatLngBounds();
    const infoWindow = new googleMaps.InfoWindow();

    window.safeRouteData.routes.forEach((route) => {
      const coordinates = route.coordinates || route.Coordinates;
      const routeName = route.name || route.Name;
      const safetyScore = route.safetyScore || route.SafetyScore;
      const color = route.color || route.Color;
      const isRecommended = route.isRecommended ?? route.IsRecommended;
      const path = coordinates.map((point) => {
        const latLng = { lat: point[0], lng: point[1] };
        bounds.extend(latLng);
        return latLng;
      });

      const line = new googleMaps.Polyline({
        path,
        geodesic: true,
        strokeColor: color,
        strokeOpacity: isRecommended ? 0.94 : 0.78,
        strokeWeight: isRecommended ? 8 : 6,
        map
      });

      line.addListener("click", (event) => {
        infoWindow.setContent(`<strong>${routeName}</strong><br>Safety Score: ${safetyScore}<br>${isRecommended ? "Recommended safer route" : "Higher-risk route"}`);
        infoWindow.setPosition(event.latLng);
        infoWindow.open(map);
      });
    });

    window.safeRouteData.crimeAreas.forEach((area) => {
      const coordinates = area.coordinates || area.Coordinates;
      const areaName = area.name || area.Name;
      const crimeScore = area.crimeScore || area.CrimeScore;
      const policePrecinct = area.policePrecinct || area.PolicePrecinct;
      const riskSignal = area.riskSignal || area.RiskSignal;
      const position = { lat: coordinates[0], lng: coordinates[1] };
      const color = riskColor(crimeScore);

      bounds.extend(position);

      const marker = new googleMaps.Marker({
        position,
        map,
        title: areaName
      });

      const circle = new googleMaps.Circle({
        strokeColor: "#ffffff",
        strokeOpacity: 0.95,
        strokeWeight: 2,
        fillColor: color,
        fillOpacity: 0.28,
        map,
        center: position,
        radius: crimeScore * 22
      });

      const content = `
        <strong>${areaName}</strong><br>
        ${policePrecinct}<br>
        Crime Score: ${crimeScore}<br>
        ${riskSignal}
      `;

      marker.addListener("click", () => {
        infoWindow.setContent(content);
        infoWindow.open(map, marker);
      });

      circle.addListener("click", (event) => {
        infoWindow.setContent(content);
        infoWindow.setPosition(event.latLng);
        infoWindow.open(map);
      });
    });

    if (!bounds.isEmpty()) {
      map.fitBounds(bounds, 34);
    }

    target.dataset.mapProvider = "Google Maps";
    return true;
  }

  function renderLeafletFallback() {
    if (!window.L) {
      target.innerHTML = "<div class='map-fallback'>Google Maps and Leaflet could not load. Check internet access, then reload to show the map.</div>";
      return;
    }

    const map = L.map(target, {
      scrollWheelZoom: false
    }).setView([-26.22, 28.0], 11);

    L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
      maxZoom: 19,
      attribution: "&copy; OpenStreetMap contributors"
    }).addTo(map);

    const bounds = [];

    window.safeRouteData.routes.forEach((route) => {
      const coordinates = route.coordinates || route.Coordinates;
      const routeName = route.name || route.Name;
      const safetyScore = route.safetyScore || route.SafetyScore;
      const color = route.color || route.Color;
      const isRecommended = route.isRecommended ?? route.IsRecommended;
      const line = L.polyline(coordinates, {
        color,
        weight: isRecommended ? 8 : 6,
        opacity: isRecommended ? 0.94 : 0.78,
        dashArray: isRecommended ? null : "8 10"
      }).addTo(map);

      line.bindPopup(`${routeName}: Safety Score ${safetyScore}<br>${isRecommended ? "Recommended safer route" : "Higher-risk route"}`);
      coordinates.forEach((point) => bounds.push(point));
    });

    window.safeRouteData.crimeAreas.forEach((area) => {
      const coordinates = area.coordinates || area.Coordinates;
      const areaName = area.name || area.Name;
      const crimeScore = area.crimeScore || area.CrimeScore;
      const policePrecinct = area.policePrecinct || area.PolicePrecinct;
      const riskSignal = area.riskSignal || area.RiskSignal;
      L.circleMarker(coordinates, {
        radius: 9,
        color: "#ffffff",
        weight: 2,
        fillColor: riskColor(crimeScore),
        fillOpacity: 0.9
      })
        .addTo(map)
        .bindPopup(`
          <strong>${areaName}</strong><br>
          ${policePrecinct}<br>
          Crime Score: ${crimeScore}<br>
          ${riskSignal}
        `);
    });

    if (bounds.length) {
      map.fitBounds(bounds, { padding: [34, 34] });
    }

    target.dataset.mapProvider = "Leaflet fallback";
  }

  if (!renderGoogleMap()) {
    renderLeafletFallback();
  }
})();
