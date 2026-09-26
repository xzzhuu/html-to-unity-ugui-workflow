window.TOWER_CRANE_PREVIEW = {
  "schemaVersion": "1.0",
  "generatedAt": "2026-09-24T06:45:09",
  "towers": [
    {
      "towerId": "TC001",
      "towerName": "塔吊1",
      "enabled": true,
      "realtime": {
        "towerId": "TC001",
        "updatedAt": "2026-09-24T14:24:35",
        "heightM": 59.6,
        "radiusM": 35.5,
        "slewAngleDeg": 274.0,
        "loadT": 0.0,
        "tiltAngleDeg": 7.7,
        "windSpeedMps": 0.5
      },
      "alarms": [
        {
          "alarmCode": "BaseCollision",
          "alarmName": "塔基碰撞报警",
          "isAlarm": false,
          "statusText": "正常",
          "alarmLevel": "Warning"
        },
        {
          "alarmCode": "Tilt",
          "alarmName": "倾斜报警",
          "isAlarm": true,
          "statusText": "报警",
          "alarmLevel": "Warning"
        },
        {
          "alarmCode": "WindSpeed",
          "alarmName": "风速报警",
          "isAlarm": false,
          "statusText": "正常",
          "alarmLevel": "Warning"
        },
        {
          "alarmCode": "Overload",
          "alarmName": "超重报警",
          "isAlarm": false,
          "statusText": "正常",
          "alarmLevel": "Warning"
        },
        {
          "alarmCode": "LeftLimit",
          "alarmName": "左限位报警",
          "isAlarm": false,
          "statusText": "正常",
          "alarmLevel": "Warning"
        },
        {
          "alarmCode": "RightLimit",
          "alarmName": "右限位报警",
          "isAlarm": false,
          "statusText": "正常",
          "alarmLevel": "Warning"
        },
        {
          "alarmCode": "FrontLimit",
          "alarmName": "前限位报警",
          "isAlarm": false,
          "statusText": "正常",
          "alarmLevel": "Warning"
        },
        {
          "alarmCode": "RearLimit",
          "alarmName": "后限位报警",
          "isAlarm": false,
          "statusText": "正常",
          "alarmLevel": "Warning"
        },
        {
          "alarmCode": "UpperLimit",
          "alarmName": "上限位报警",
          "isAlarm": false,
          "statusText": "正常",
          "alarmLevel": "Warning"
        }
      ],
      "history": [
        {
          "sampleTime": "2026-09-23T16:00:00",
          "heightM": 37.6,
          "radiusM": 4.8,
          "loadT": 0.0,
          "windSpeedMps": 0.7
        },
        {
          "sampleTime": "2026-09-23T18:00:00",
          "heightM": 39.4,
          "radiusM": 2.3,
          "loadT": 0.0,
          "windSpeedMps": 0.6
        },
        {
          "sampleTime": "2026-09-23T20:00:00",
          "heightM": 40.8,
          "radiusM": 3.5,
          "loadT": 0.0,
          "windSpeedMps": 0.4
        },
        {
          "sampleTime": "2026-09-23T22:00:00",
          "heightM": 41.0,
          "radiusM": 2.0,
          "loadT": 0.0,
          "windSpeedMps": 0.4
        },
        {
          "sampleTime": "2026-09-24T00:00:00",
          "heightM": 43.9,
          "radiusM": 2.6,
          "loadT": 0.0,
          "windSpeedMps": 0.3
        },
        {
          "sampleTime": "2026-09-24T02:00:00",
          "heightM": 43.2,
          "radiusM": 4.4,
          "loadT": 0.0,
          "windSpeedMps": 0.3
        },
        {
          "sampleTime": "2026-09-24T04:00:00",
          "heightM": 43.5,
          "radiusM": 5.8,
          "loadT": 0.0,
          "windSpeedMps": 0.6
        },
        {
          "sampleTime": "2026-09-24T06:00:00",
          "heightM": 45.4,
          "radiusM": 6.0,
          "loadT": 0.0,
          "windSpeedMps": 0.5
        },
        {
          "sampleTime": "2026-09-24T08:00:00",
          "heightM": 46.2,
          "radiusM": 8.9,
          "loadT": 0.0,
          "windSpeedMps": 0.3
        },
        {
          "sampleTime": "2026-09-24T10:00:00",
          "heightM": 48.3,
          "radiusM": 8.2,
          "loadT": 0.0,
          "windSpeedMps": 0.7
        },
        {
          "sampleTime": "2026-09-24T12:00:00",
          "heightM": 48.8,
          "radiusM": 8.5,
          "loadT": 0.0,
          "windSpeedMps": 0.7
        },
        {
          "sampleTime": "2026-09-24T14:00:00",
          "heightM": 50.0,
          "radiusM": 10.4,
          "loadT": 0.0,
          "windSpeedMps": 0.4
        }
      ],
      "charts": [
        {
          "chartCode": "Height",
          "chartName": "高度历史趋势",
          "metricCode": "HeightM",
          "imagePath": "TowerCrane/Charts/TC001/height.png"
        },
        {
          "chartCode": "Radius",
          "chartName": "幅度历史趋势",
          "metricCode": "RadiusM",
          "imagePath": "TowerCrane/Charts/TC001/radius.png"
        },
        {
          "chartCode": "Load",
          "chartName": "吊重历史趋势",
          "metricCode": "LoadT",
          "imagePath": "TowerCrane/Charts/TC001/load.png"
        },
        {
          "chartCode": "WindSpeed",
          "chartName": "风速历史趋势",
          "metricCode": "WindSpeedMps",
          "imagePath": "TowerCrane/Charts/TC001/wind_speed.png"
        }
      ]
    },
    {
      "towerId": "TC002",
      "towerName": "塔吊2",
      "enabled": true,
      "realtime": {
        "towerId": "TC002",
        "updatedAt": "2026-09-24T14:24:35",
        "heightM": 30.4,
        "radiusM": 137.0,
        "slewAngleDeg": 217.0,
        "loadT": 0.0,
        "tiltAngleDeg": 5.4,
        "windSpeedMps": 3.6
      },
      "alarms": [
        {
          "alarmCode": "BaseCollision",
          "alarmName": "塔基碰撞报警",
          "isAlarm": false,
          "statusText": "正常",
          "alarmLevel": "Warning"
        },
        {
          "alarmCode": "Tilt",
          "alarmName": "倾斜报警",
          "isAlarm": false,
          "statusText": "正常",
          "alarmLevel": "Warning"
        },
        {
          "alarmCode": "WindSpeed",
          "alarmName": "风速报警",
          "isAlarm": false,
          "statusText": "正常",
          "alarmLevel": "Warning"
        },
        {
          "alarmCode": "Overload",
          "alarmName": "超重报警",
          "isAlarm": false,
          "statusText": "正常",
          "alarmLevel": "Warning"
        },
        {
          "alarmCode": "LeftLimit",
          "alarmName": "左限位报警",
          "isAlarm": false,
          "statusText": "正常",
          "alarmLevel": "Warning"
        },
        {
          "alarmCode": "RightLimit",
          "alarmName": "右限位报警",
          "isAlarm": false,
          "statusText": "正常",
          "alarmLevel": "Warning"
        },
        {
          "alarmCode": "FrontLimit",
          "alarmName": "前限位报警",
          "isAlarm": false,
          "statusText": "正常",
          "alarmLevel": "Warning"
        },
        {
          "alarmCode": "RearLimit",
          "alarmName": "后限位报警",
          "isAlarm": false,
          "statusText": "正常",
          "alarmLevel": "Warning"
        },
        {
          "alarmCode": "UpperLimit",
          "alarmName": "上限位报警",
          "isAlarm": false,
          "statusText": "正常",
          "alarmLevel": "Warning"
        }
      ],
      "history": [
        {
          "sampleTime": "2026-09-23T16:00:00",
          "heightM": 38.0,
          "radiusM": 138.0,
          "loadT": 0.0,
          "windSpeedMps": 3.9
        },
        {
          "sampleTime": "2026-09-23T18:00:00",
          "heightM": 40.7,
          "radiusM": 140.7,
          "loadT": 0.0,
          "windSpeedMps": 3.9
        },
        {
          "sampleTime": "2026-09-23T20:00:00",
          "heightM": 39.8,
          "radiusM": 139.8,
          "loadT": 0.0,
          "windSpeedMps": 3.8
        },
        {
          "sampleTime": "2026-09-23T22:00:00",
          "heightM": 37.3,
          "radiusM": 137.3,
          "loadT": 0.0,
          "windSpeedMps": 3.8
        },
        {
          "sampleTime": "2026-09-24T00:00:00",
          "heightM": 34.9,
          "radiusM": 134.9,
          "loadT": 0.0,
          "windSpeedMps": 4.1
        },
        {
          "sampleTime": "2026-09-24T02:00:00",
          "heightM": 37.0,
          "radiusM": 137.0,
          "loadT": 0.0,
          "windSpeedMps": 4.0
        },
        {
          "sampleTime": "2026-09-24T04:00:00",
          "heightM": 37.6,
          "radiusM": 137.6,
          "loadT": 0.0,
          "windSpeedMps": 3.8
        },
        {
          "sampleTime": "2026-09-24T06:00:00",
          "heightM": 39.4,
          "radiusM": 139.4,
          "loadT": 0.0,
          "windSpeedMps": 4.2
        },
        {
          "sampleTime": "2026-09-24T08:00:00",
          "heightM": 40.8,
          "radiusM": 140.8,
          "loadT": 0.0,
          "windSpeedMps": 4.2
        },
        {
          "sampleTime": "2026-09-24T10:00:00",
          "heightM": 41.0,
          "radiusM": 141.0,
          "loadT": 0.0,
          "windSpeedMps": 3.9
        },
        {
          "sampleTime": "2026-09-24T12:00:00",
          "heightM": 43.9,
          "radiusM": 143.9,
          "loadT": 0.0,
          "windSpeedMps": 3.6
        },
        {
          "sampleTime": "2026-09-24T14:00:00",
          "heightM": 38.5,
          "radiusM": 143.2,
          "loadT": 0.0,
          "windSpeedMps": 3.3
        }
      ],
      "charts": [
        {
          "chartCode": "Height",
          "chartName": "高度历史趋势",
          "metricCode": "HeightM",
          "imagePath": "TowerCrane/Charts/TC002/height.png"
        },
        {
          "chartCode": "Radius",
          "chartName": "幅度历史趋势",
          "metricCode": "RadiusM",
          "imagePath": "TowerCrane/Charts/TC002/radius.png"
        },
        {
          "chartCode": "Load",
          "chartName": "吊重历史趋势",
          "metricCode": "LoadT",
          "imagePath": "TowerCrane/Charts/TC002/load.png"
        },
        {
          "chartCode": "WindSpeed",
          "chartName": "风速历史趋势",
          "metricCode": "WindSpeedMps",
          "imagePath": "TowerCrane/Charts/TC002/wind_speed.png"
        }
      ]
    },
    {
      "towerId": "TC003",
      "towerName": "塔吊3",
      "enabled": true,
      "realtime": {
        "towerId": "TC003",
        "updatedAt": "2026-09-24T14:24:35",
        "heightM": 7.4,
        "radiusM": 36.5,
        "slewAngleDeg": 147.0,
        "loadT": 0.0,
        "tiltAngleDeg": 8.9,
        "windSpeedMps": 1.2
      },
      "alarms": [
        {
          "alarmCode": "BaseCollision",
          "alarmName": "塔基碰撞报警",
          "isAlarm": false,
          "statusText": "正常",
          "alarmLevel": "Warning"
        },
        {
          "alarmCode": "Tilt",
          "alarmName": "倾斜报警",
          "isAlarm": false,
          "statusText": "正常",
          "alarmLevel": "Warning"
        },
        {
          "alarmCode": "WindSpeed",
          "alarmName": "风速报警",
          "isAlarm": false,
          "statusText": "正常",
          "alarmLevel": "Warning"
        },
        {
          "alarmCode": "Overload",
          "alarmName": "超重报警",
          "isAlarm": false,
          "statusText": "正常",
          "alarmLevel": "Warning"
        },
        {
          "alarmCode": "LeftLimit",
          "alarmName": "左限位报警",
          "isAlarm": false,
          "statusText": "正常",
          "alarmLevel": "Warning"
        },
        {
          "alarmCode": "RightLimit",
          "alarmName": "右限位报警",
          "isAlarm": false,
          "statusText": "正常",
          "alarmLevel": "Warning"
        },
        {
          "alarmCode": "FrontLimit",
          "alarmName": "前限位报警",
          "isAlarm": false,
          "statusText": "正常",
          "alarmLevel": "Warning"
        },
        {
          "alarmCode": "RearLimit",
          "alarmName": "后限位报警",
          "isAlarm": false,
          "statusText": "正常",
          "alarmLevel": "Warning"
        },
        {
          "alarmCode": "UpperLimit",
          "alarmName": "上限位报警",
          "isAlarm": false,
          "statusText": "正常",
          "alarmLevel": "Warning"
        }
      ],
      "history": [
        {
          "sampleTime": "2026-09-23T16:00:00",
          "heightM": 31.7,
          "radiusM": 39.8,
          "loadT": 0.0,
          "windSpeedMps": 2.1
        },
        {
          "sampleTime": "2026-09-23T18:00:00",
          "heightM": 30.0,
          "radiusM": 37.3,
          "loadT": 0.0,
          "windSpeedMps": 2.0
        },
        {
          "sampleTime": "2026-09-23T20:00:00",
          "heightM": 30.0,
          "radiusM": 34.9,
          "loadT": 0.0,
          "windSpeedMps": 1.8
        },
        {
          "sampleTime": "2026-09-23T22:00:00",
          "heightM": 30.7,
          "radiusM": 37.0,
          "loadT": 0.0,
          "windSpeedMps": 2.2
        },
        {
          "sampleTime": "2026-09-24T00:00:00",
          "heightM": 31.0,
          "radiusM": 37.6,
          "loadT": 0.0,
          "windSpeedMps": 2.2
        },
        {
          "sampleTime": "2026-09-24T02:00:00",
          "heightM": 30.0,
          "radiusM": 39.4,
          "loadT": 0.0,
          "windSpeedMps": 1.9
        },
        {
          "sampleTime": "2026-09-24T04:00:00",
          "heightM": 30.4,
          "radiusM": 40.8,
          "loadT": 0.0,
          "windSpeedMps": 1.6
        },
        {
          "sampleTime": "2026-09-24T06:00:00",
          "heightM": 32.0,
          "radiusM": 41.0,
          "loadT": 0.0,
          "windSpeedMps": 1.3
        },
        {
          "sampleTime": "2026-09-24T08:00:00",
          "heightM": 30.0,
          "radiusM": 43.9,
          "loadT": 0.0,
          "windSpeedMps": 1.4
        },
        {
          "sampleTime": "2026-09-24T10:00:00",
          "heightM": 31.5,
          "radiusM": 43.2,
          "loadT": 0.0,
          "windSpeedMps": 1.6
        },
        {
          "sampleTime": "2026-09-24T12:00:00",
          "heightM": 32.5,
          "radiusM": 43.5,
          "loadT": 0.0,
          "windSpeedMps": 1.6
        },
        {
          "sampleTime": "2026-09-24T14:00:00",
          "heightM": 31.7,
          "radiusM": 45.4,
          "loadT": 0.0,
          "windSpeedMps": 1.2
        }
      ],
      "charts": [
        {
          "chartCode": "Height",
          "chartName": "高度历史趋势",
          "metricCode": "HeightM",
          "imagePath": "TowerCrane/Charts/TC003/height.png"
        },
        {
          "chartCode": "Radius",
          "chartName": "幅度历史趋势",
          "metricCode": "RadiusM",
          "imagePath": "TowerCrane/Charts/TC003/radius.png"
        },
        {
          "chartCode": "Load",
          "chartName": "吊重历史趋势",
          "metricCode": "LoadT",
          "imagePath": "TowerCrane/Charts/TC003/load.png"
        },
        {
          "chartCode": "WindSpeed",
          "chartName": "风速历史趋势",
          "metricCode": "WindSpeedMps",
          "imagePath": "TowerCrane/Charts/TC003/wind_speed.png"
        }
      ]
    },
    {
      "towerId": "TC004",
      "towerName": "塔吊4",
      "enabled": true,
      "realtime": {
        "towerId": "TC004",
        "updatedAt": "2026-09-24T14:24:35",
        "heightM": 50.0,
        "radiusM": 11.3,
        "slewAngleDeg": 327.0,
        "loadT": 0.0,
        "tiltAngleDeg": 0.5,
        "windSpeedMps": 0.5
      },
      "alarms": [
        {
          "alarmCode": "BaseCollision",
          "alarmName": "塔基碰撞报警",
          "isAlarm": false,
          "statusText": "正常",
          "alarmLevel": "Warning"
        },
        {
          "alarmCode": "Tilt",
          "alarmName": "倾斜报警",
          "isAlarm": false,
          "statusText": "正常",
          "alarmLevel": "Warning"
        },
        {
          "alarmCode": "WindSpeed",
          "alarmName": "风速报警",
          "isAlarm": false,
          "statusText": "正常",
          "alarmLevel": "Warning"
        },
        {
          "alarmCode": "Overload",
          "alarmName": "超重报警",
          "isAlarm": false,
          "statusText": "正常",
          "alarmLevel": "Warning"
        },
        {
          "alarmCode": "LeftLimit",
          "alarmName": "左限位报警",
          "isAlarm": false,
          "statusText": "正常",
          "alarmLevel": "Warning"
        },
        {
          "alarmCode": "RightLimit",
          "alarmName": "右限位报警",
          "isAlarm": false,
          "statusText": "正常",
          "alarmLevel": "Warning"
        },
        {
          "alarmCode": "FrontLimit",
          "alarmName": "前限位报警",
          "isAlarm": false,
          "statusText": "正常",
          "alarmLevel": "Warning"
        },
        {
          "alarmCode": "RearLimit",
          "alarmName": "后限位报警",
          "isAlarm": false,
          "statusText": "正常",
          "alarmLevel": "Warning"
        },
        {
          "alarmCode": "UpperLimit",
          "alarmName": "上限位报警",
          "isAlarm": false,
          "statusText": "正常",
          "alarmLevel": "Warning"
        }
      ],
      "history": [
        {
          "sampleTime": "2026-09-23T16:00:00",
          "heightM": 37.6,
          "radiusM": 4.8,
          "loadT": 0.0,
          "windSpeedMps": 0.7
        },
        {
          "sampleTime": "2026-09-23T18:00:00",
          "heightM": 39.4,
          "radiusM": 2.3,
          "loadT": 0.0,
          "windSpeedMps": 0.6
        },
        {
          "sampleTime": "2026-09-23T20:00:00",
          "heightM": 40.8,
          "radiusM": 3.5,
          "loadT": 0.0,
          "windSpeedMps": 0.4
        },
        {
          "sampleTime": "2026-09-23T22:00:00",
          "heightM": 41.0,
          "radiusM": 2.0,
          "loadT": 0.0,
          "windSpeedMps": 0.4
        },
        {
          "sampleTime": "2026-09-24T00:00:00",
          "heightM": 43.9,
          "radiusM": 2.6,
          "loadT": 0.0,
          "windSpeedMps": 0.3
        },
        {
          "sampleTime": "2026-09-24T02:00:00",
          "heightM": 43.2,
          "radiusM": 4.4,
          "loadT": 0.0,
          "windSpeedMps": 0.3
        },
        {
          "sampleTime": "2026-09-24T04:00:00",
          "heightM": 43.5,
          "radiusM": 5.8,
          "loadT": 0.0,
          "windSpeedMps": 0.6
        },
        {
          "sampleTime": "2026-09-24T06:00:00",
          "heightM": 45.4,
          "radiusM": 6.0,
          "loadT": 0.0,
          "windSpeedMps": 0.5
        },
        {
          "sampleTime": "2026-09-24T08:00:00",
          "heightM": 46.2,
          "radiusM": 8.9,
          "loadT": 0.0,
          "windSpeedMps": 0.3
        },
        {
          "sampleTime": "2026-09-24T10:00:00",
          "heightM": 48.3,
          "radiusM": 8.2,
          "loadT": 0.0,
          "windSpeedMps": 0.7
        },
        {
          "sampleTime": "2026-09-24T12:00:00",
          "heightM": 48.8,
          "radiusM": 8.5,
          "loadT": 0.0,
          "windSpeedMps": 0.7
        },
        {
          "sampleTime": "2026-09-24T14:00:00",
          "heightM": 50.0,
          "radiusM": 10.4,
          "loadT": 0.0,
          "windSpeedMps": 0.4
        }
      ],
      "charts": [
        {
          "chartCode": "Height",
          "chartName": "高度历史趋势",
          "metricCode": "HeightM",
          "imagePath": "TowerCrane/Charts/TC004/height.png"
        },
        {
          "chartCode": "Radius",
          "chartName": "幅度历史趋势",
          "metricCode": "RadiusM",
          "imagePath": "TowerCrane/Charts/TC004/radius.png"
        },
        {
          "chartCode": "Load",
          "chartName": "吊重历史趋势",
          "metricCode": "LoadT",
          "imagePath": "TowerCrane/Charts/TC004/load.png"
        },
        {
          "chartCode": "WindSpeed",
          "chartName": "风速历史趋势",
          "metricCode": "WindSpeedMps",
          "imagePath": "TowerCrane/Charts/TC004/wind_speed.png"
        }
      ]
    }
  ]
};
