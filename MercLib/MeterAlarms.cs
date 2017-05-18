using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization.Json;

namespace MercuryMeter
{
    [Serializable]
    public class MetersParameter 
    {
        public delegate void Changed ();
        public event Changed ParametrUpdated;
        public MetersParameter() {
            minalarm = false;
            maxalarm = false;
        }
        public MetersParameter(float min, float max, float hist)
        {
            MinValue = min;
            MaxValue = max;
            Hist = hist;
            minalarm = false;
            maxalarm = false;
        }
        public string alias{set; get;} 
        //public string AlarmName {set; get;}
        public float MaxValue { set; get; }
        public float MinValue { set; get; }
        public float Hist { set; get; }
        private bool minalarm;
        private bool maxalarm;
        public bool ComAlarm { get { return MinValueAlarm || MaxValueAlarm ; } }
        public virtual bool MinValueAlarm { get{
             return minalarm;
        } }
        public virtual bool MaxValueAlarm { get{
             return maxalarm;
        } }
        public virtual void RefreshData()
        {
            if (null != ParametrUpdated)
            {
                ParametrUpdated();
            }
            if ((MinValue == 0) && (MaxValue == 0))
            {
                return;
            }
             if (parametr < (MinValue - Hist))
             {
                 minalarm = true;
             }
             if (parametr > (MinValue + Hist))
             {
                 minalarm = false;
             }
             if (parametr < (MaxValue - Hist))
             {
                 maxalarm = false;
             }
             if (parametr > (MaxValue + Hist))
             {
                 maxalarm = true;
             }
             
        }
        float parametr;
        //private   float min;
        //private   float max;
        public virtual float Value { 
            set{
            parametr = value;
            RefreshData();
            }
            get
            {
            //RefreshData(); риск зацикливания
            return parametr;
            }
        }

        public void CopyLimits(MetersParameter ext_par)
        {
            this.MinValue = ext_par.MinValue;
            this.MaxValue = ext_par.MaxValue;
            this.Hist = ext_par.Hist;
        }
    }
    [Serializable]
    public class Phase {
        public MetersParameter voltage;
        public MetersParameter current;
        public MetersParameter power_factor; // 4 элемента - По сумме фаз + 1ф+2ф+3ф
        public PhasePower power;

        public Phase(float maxcur){
            voltage = new MetersParameter(198,242,2);
            current = new MetersParameter(-0.1F,maxcur,1);
            power_factor = new MetersParameter(0.8F,1.5F,0.01F);
            power = new PhasePower(current, voltage, 10000, 500);
            InitNames();
        }
        public Phase(){
            voltage = new MetersParameter(198,242,2);
            current = new MetersParameter(-0.1F,20,1);
            power_factor = new MetersParameter(0.8F,1.5F,0.01F);
            power = new PhasePower(current, voltage, 10000, 500);
            InitNames();
        }
        protected void InitNames(){
            current.alias = "cur";
            voltage.alias = "volt";
            power_factor.alias = "powfact" ;
            power.alias = "power";
        }
        [Serializable]
        public class PhasePower  : MetersParameter 
        {
            public MetersParameter voltage;
            public MetersParameter current;
            public PhasePower() { }
            public PhasePower(MetersParameter current, MetersParameter voltage, float max, float hist, float min = 0  ): base (min, max, hist)
            {
                this.MinValue = 0;
                this.MaxValue = max;
                this.voltage = voltage;
                this.current = current;
            }
            public override float Value {
                 get{
                    this.RefreshData();
                    return this.voltage.Value * this.current.Value;
                    }
                }
            }
        }

        
}



