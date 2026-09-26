(function(){
  var ds=window.TOWER_CRANE_PREVIEW;
  if(!ds||!ds.towers||!ds.towers.length)return;
  var currentId=ds.towers[0].towerId;
  function one(sel,root){return (root||document).querySelector(sel)}
  function all(sel,root){return Array.prototype.slice.call((root||document).querySelectorAll(sel))}
  function setText(key,value){var el=one('[data-binding="'+key+'"]');if(el)el.textContent=value}

  var deviceList=one('[data-binding="Device.List"]');
  var deviceTemplate=document.getElementById('DeviceItemTemplate');
  var alarmList=one('[data-binding="Alarm.List"]');
  var alarmTemplate=document.getElementById('AlarmItemTemplate');

  function buildDeviceList(){
    if(!deviceList||!deviceTemplate)return;
    all('.device-item',deviceList).forEach(function(x){x.remove()});
    ds.towers.forEach(function(t,i){
      var node=deviceTemplate.content.firstElementChild.cloneNode(true);
      node.style.top=(i*64)+'px';node.setAttribute('data-device-id',t.towerId);
      one('.device-code',node).textContent=t.towerId;
      one('.device-name',node).textContent=t.towerName;
      node.addEventListener('click',function(){render(t.towerId)});
      deviceList.appendChild(node);
    });
    setText('Device.Count','共 '+ds.towers.length+' 台');
  }

  function buildAlarmList(t){
    if(!alarmList||!alarmTemplate)return;
    all('.alarm-item',alarmList).forEach(function(x){x.remove()});
    (t.alarms||[]).forEach(function(a,i){
      var node=alarmTemplate.content.firstElementChild.cloneNode(true);
      node.style.top=(i*64)+'px';node.setAttribute('data-alarm-code',a.alarmCode);
      one('.alarm-name',node).textContent=a.alarmName;
      applyAlarmVisual(node,!!a.isAlarm);
      alarmList.appendChild(node);
    });
  }

  function applyAlarmVisual(item,isAlarm){
    if(!item)return;
    var dot=one('.alarm-dot',item),pill=one('.status-pill',item),text=one('.status-pill-text',item);
    if(dot){dot.classList.toggle('dot-alarm',isAlarm);dot.classList.toggle('dot-normal',!isAlarm)}
    if(pill){pill.classList.toggle('status-alarm',isAlarm);pill.classList.toggle('status-normal',!isAlarm);pill.style.backgroundImage='url("assets/sprites/'+(isAlarm?'pill_red.png':'pill_green.png')+'")'}
    if(text)text.textContent=isAlarm?'报警':'正常';
  }

  function chart(key,id,file){var el=one('[data-binding="'+key+'"]');if(el)el.src='assets/textures/charts/'+id+'/'+file}
  function render(id){
    var t=ds.towers.find(function(x){return x.towerId===id})||ds.towers[0];currentId=t.towerId;var r=t.realtime||{};
    setText('Header.DeviceAndUpdate','当前设备：'+t.towerId+' / '+t.towerName+'　|　更新时间：'+String(r.updatedAt||'').replace('T',' '));
    setText('Realtime.HeightM',Number(r.heightM||0).toFixed(1));setText('Realtime.RadiusM',Number(r.radiusM||0).toFixed(1));
    setText('Realtime.SlewAngleDeg',Number(r.slewAngleDeg||0).toFixed(1));setText('Realtime.LoadT',Number(r.loadT||0).toFixed(1));
    setText('Realtime.TiltAngleDeg',Number(r.tiltAngleDeg||0).toFixed(1));setText('Realtime.WindSpeedMps',Number(r.windSpeedMps||0).toFixed(1));
    var active=(t.alarms||[]).filter(function(a){return a.isAlarm}).length,normal=(t.alarms||[]).length-active;
    setText('Alarm.NormalCount',String(normal));setText('Alarm.ActiveCount',String(active));setText('Alarm.Count','共 '+(t.alarms||[]).length+' 项');
    buildAlarmList(t);
    chart('History.Height',t.towerId,'height.png');chart('History.Radius',t.towerId,'radius.png');chart('History.Load',t.towerId,'load.png');chart('History.WindSpeed',t.towerId,'wind_speed.png');
    all('.device-item',deviceList).forEach(function(b){b.classList.toggle('selected',b.getAttribute('data-device-id')===t.towerId)});
  }

  buildDeviceList();
  var input=one('[data-binding="Device.Search"] input')||one('[data-binding="Device.Search"]');
  if(input&&input.addEventListener){input.addEventListener('input',function(){
    var q=(input.value||'').trim().toLowerCase();var y=0;
    all('.device-item',deviceList).forEach(function(b){
      var show=!q||b.textContent.toLowerCase().indexOf(q)>=0;b.classList.toggle('hidden',!show);
      if(show){b.style.top=y+'px';y+=64}
    });
  })}
  var refresh=one('[data-binding="Action.Refresh"]');if(refresh)refresh.addEventListener('click',function(){render(currentId)});
  render(currentId);
})();
